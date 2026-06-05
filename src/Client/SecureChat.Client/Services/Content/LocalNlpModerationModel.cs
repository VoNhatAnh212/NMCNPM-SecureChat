#nullable enable

using System.Text.RegularExpressions;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SecureChat.Client.Services.Content;

/// <summary>
/// Runs a real local ONNX text-classification model for toxicity moderation.
/// </summary>
public sealed class LocalNlpModerationModel : IDisposable
{
    private const int MaxSequenceLength = 128;
    private const double ToxicityThreshold = 0.65D;
    private const double AttributionThreshold = 0.08D;
    private const string Mask = "****";
    private const string VietnameseCharacters =
        "ăâđêôơưĂÂĐÊÔƠƯáàảãạÁÀẢÃẠắằẳẵặẮẰẲẴẶấầẩẫậẤẦẨẪẬéèẻẽẹÉÈẺẼẸếềểễệẾỀỂỄỆíìỉĩịÍÌỈĨỊóòỏõọÓÒỎÕỌốồổỗộỐỒỔỖỘớờởỡợỚỜỞỠỢúùủũụÚÙỦŨỤứừửữựỨỪỬỮỰýỳỷỹỵÝỲỶỸỴ";
    private static readonly Lazy<LocalNlpModerationModel?> EnglishInstance = new(() => Create(
        "AiModeration",
        "toxic-bert-quantized.onnx",
        lowercaseTokenizer: true));
    private static readonly Lazy<LocalNlpModerationModel?> MultilingualInstance = new(() => Create(
        "AiModerationVi",
        "bert-multilingual-toxicity-int8.onnx",
        lowercaseTokenizer: false));
    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}_']+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex[] VietnameseProfanityPatterns =
    {
        CreateVietnameseProfanityPattern("cặc"),
        CreateVietnameseProfanityPattern("cac"),
        CreateVietnameseProfanityPattern("lồn"),
        CreateVietnameseProfanityPattern("lon"),
        CreateVietnameseProfanityPattern("loz"),
        CreateVietnameseProfanityPattern("địt"),
        CreateVietnameseProfanityPattern("dit"),
        CreateVietnameseProfanityPattern("đụ"),
        CreateVietnameseProfanityPattern("du"),
        CreateVietnameseProfanityPattern("đéo"),
        CreateVietnameseProfanityPattern("deo"),
        CreateVietnameseProfanityPattern("đm"),
        CreateVietnameseProfanityPattern("dm"),
        CreateVietnameseProfanityPattern("đmm"),
        CreateVietnameseProfanityPattern("dmm"),
        CreateVietnameseProfanityPattern("vcl"),
        CreateVietnameseProfanityPattern("vãi lồn"),
        CreateVietnameseProfanityPattern("vai lon"),
        CreateVietnameseProfanityPattern("óc chó"),
        CreateVietnameseProfanityPattern("oc cho")
    };
    private readonly InferenceSession _session;
    private readonly BertWordPieceTokenizer _tokenizer;

    private LocalNlpModerationModel(string modelPath, string vocabPath, bool lowercaseTokenizer)
    {
        _session = new InferenceSession(modelPath);
        _tokenizer = new BertWordPieceTokenizer(vocabPath, lowercaseTokenizer);
    }

    /// <summary>Gets a value indicating whether the local model files can be loaded.</summary>
    public static bool IsAvailable => EnglishInstance.Value is not null || MultilingualInstance.Value is not null;

    /// <summary>Returns a moderation result for the provided plaintext.</summary>
    public static ModerationResult Analyze(string text)
    {
        if (ContainsHighPrecisionVietnameseProfanity(text))
        {
            return new ModerationResult(true, 0.99D);
        }

        var prediction = PredictWithBestAvailableModel(text);
        if (prediction is null)
        {
            return new ModerationResult(false, 0D);
        }

        return new ModerationResult(prediction.Score >= ToxicityThreshold, prediction.Score);
    }

    /// <summary>Masks words that the local model attributes most strongly to toxicity.</summary>
    public static string MaskOffensiveTerms(string text)
    {
        var profanityMasked = MaskHighPrecisionVietnameseProfanity(text);
        if (!string.Equals(profanityMasked, text, StringComparison.Ordinal))
        {
            return profanityMasked;
        }

        var prediction = PredictWithBestAvailableModel(text);
        if (prediction is null)
        {
            return text;
        }

        var model = prediction.Model;
        var baseScore = prediction.Score;
        if (baseScore < ToxicityThreshold)
        {
            return text;
        }

        var candidates = WordRegex.Matches(text)
            .Cast<Match>()
            .Where(match => match.Length > 1)
            .Select(match =>
            {
                var withoutWord = text.Remove(match.Index, match.Length).Insert(match.Index, "[MASK]");
                var scoreWithoutWord = model.PredictToxicityScore(withoutWord);
                return new AttributedSpan(match.Index, match.Length, baseScore - scoreWithoutWord);
            })
            .Where(span => span.Contribution >= AttributionThreshold)
            .OrderByDescending(span => span.Contribution)
            .Take(4)
            .OrderByDescending(span => span.Index)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = WordRegex.Matches(text)
                .Cast<Match>()
                .Where(match => match.Length > 1)
                .Select(match =>
                {
                    var withoutWord = text.Remove(match.Index, match.Length).Insert(match.Index, "[MASK]");
                    var scoreWithoutWord = model.PredictToxicityScore(withoutWord);
                    return new AttributedSpan(match.Index, match.Length, baseScore - scoreWithoutWord);
                })
                .OrderByDescending(span => span.Contribution)
                .Take(1)
                .OrderByDescending(span => span.Index)
                .ToList();
        }

        var filtered = text;
        foreach (var span in candidates)
        {
            if (span.Index >= 0 && span.Index + span.Length <= filtered.Length)
            {
                filtered = filtered.Remove(span.Index, span.Length).Insert(span.Index, Mask);
            }
        }

        return filtered;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _session.Dispose();
    }

    private double PredictToxicityScore(string text)
    {
        var encoded = _tokenizer.Encode(text, MaxSequenceLength);
        var inputIds = new DenseTensor<long>(encoded.InputIds, new[] { 1, MaxSequenceLength });
        var attentionMask = new DenseTensor<long>(encoded.AttentionMask, new[] { 1, MaxSequenceLength });
        var tokenTypeIds = new DenseTensor<long>(encoded.TokenTypeIds, new[] { 1, MaxSequenceLength });

        var inputs = new List<NamedOnnxValue>();
        foreach (var inputName in _session.InputMetadata.Keys)
        {
            if (string.Equals(inputName, "input_ids", StringComparison.OrdinalIgnoreCase))
            {
                inputs.Add(NamedOnnxValue.CreateFromTensor(inputName, inputIds));
            }
            else if (string.Equals(inputName, "attention_mask", StringComparison.OrdinalIgnoreCase))
            {
                inputs.Add(NamedOnnxValue.CreateFromTensor(inputName, attentionMask));
            }
            else if (string.Equals(inputName, "token_type_ids", StringComparison.OrdinalIgnoreCase))
            {
                inputs.Add(NamedOnnxValue.CreateFromTensor(inputName, tokenTypeIds));
            }
        }

        using var results = _session.Run(inputs);
        var logits = results.First().AsEnumerable<float>().Select(value => (double)value).ToArray();
        if (logits.Length == 0)
        {
            return 0D;
        }

        if (logits.Length == 2)
        {
            var exp0 = Math.Exp(logits[0]);
            var exp1 = Math.Exp(logits[1]);
            return exp1 / (exp0 + exp1);
        }

        return logits.Select(Sigmoid).Max();
    }

    private static double Sigmoid(double value)
    {
        return 1D / (1D + Math.Exp(-value));
    }

    private static ModelPrediction? PredictWithBestAvailableModel(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var candidates = GetCandidateModels(text);

        ModelPrediction? best = null;
        foreach (var model in candidates)
        {
            var score = model.PredictToxicityScore(text);
            if (best is null || score > best.Score)
            {
                best = new ModelPrediction(model, score);
            }
        }

        return best;
    }

    private static IEnumerable<LocalNlpModerationModel> GetCandidateModels(string text)
    {
        var english = EnglishInstance.Value;
        var multilingual = MultilingualInstance.Value;
        if (ContainsVietnameseCharacters(text))
        {
            if (multilingual is not null)
            {
                yield return multilingual;
            }

            if (english is not null)
            {
                yield return english;
            }

            yield break;
        }

        if (english is not null)
        {
            yield return english;
            yield break;
        }

        if (multilingual is not null)
        {
            yield return multilingual;
        }
    }

    private static bool ContainsVietnameseCharacters(string text)
    {
        return text.Any(character => VietnameseCharacters.Contains(character, StringComparison.Ordinal));
    }

    private static bool ContainsHighPrecisionVietnameseProfanity(string text)
    {
        return !string.IsNullOrWhiteSpace(text)
            && VietnameseProfanityPatterns.Any(pattern => pattern.IsMatch(text));
    }

    private static string MaskHighPrecisionVietnameseProfanity(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var filtered = text;
        foreach (var pattern in VietnameseProfanityPatterns)
        {
            filtered = pattern.Replace(filtered, match =>
            {
                var visibleSpaces = match.Value.Count(char.IsWhiteSpace);
                return visibleSpaces == 0
                    ? Mask
                    : string.Join(' ', Enumerable.Repeat(Mask, visibleSpaces + 1));
            });
        }

        return filtered;
    }

    private static Regex CreateVietnameseProfanityPattern(string phrase)
    {
        var escaped = Regex.Escape(phrase).Replace("\\ ", @"\s+");
        return new Regex(
            $@"(?<![\p{{L}}\p{{N}}_]){escaped}(?![\p{{L}}\p{{N}}_])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    }

    private static LocalNlpModerationModel? Create(
        string modelDirectoryName,
        string modelFileName,
        bool lowercaseTokenizer)
    {
        var modelDir = ResolveModelDirectory(modelDirectoryName);
        var modelPath = Path.Combine(modelDir, modelFileName);
        var vocabPath = Path.Combine(modelDir, "vocab.txt");
        return File.Exists(modelPath) && File.Exists(vocabPath)
            ? new LocalNlpModerationModel(modelPath, vocabPath, lowercaseTokenizer)
            : null;
    }

    private static string ResolveModelDirectory(string directoryName)
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "Models", directoryName);
        if (Directory.Exists(outputPath))
        {
            return outputPath;
        }

        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Models", directoryName));
    }

    /// <summary>Represents a local model moderation result.</summary>
    public sealed record ModerationResult(bool IsOffensive, double Score);

    private sealed record ModelPrediction(LocalNlpModerationModel Model, double Score);

    private sealed record AttributedSpan(int Index, int Length, double Contribution);

    private sealed class BertWordPieceTokenizer
    {
        private const string UnknownToken = "[UNK]";
        private const string ClassificationToken = "[CLS]";
        private const string SeparatorToken = "[SEP]";
        private const string PaddingToken = "[PAD]";
        private readonly Dictionary<string, int> _vocabulary;
        private readonly int _unknownId;
        private readonly int _classificationId;
        private readonly int _separatorId;
        private readonly int _paddingId;
        private readonly bool _lowercase;

        public BertWordPieceTokenizer(string vocabPath, bool lowercase)
        {
            _lowercase = lowercase;
            _vocabulary = File.ReadLines(vocabPath)
                .Select((token, index) => new { Token = token.Trim(), Index = index })
                .Where(item => item.Token.Length > 0)
                .ToDictionary(item => item.Token, item => item.Index, StringComparer.Ordinal);
            _unknownId = GetRequiredTokenId(UnknownToken);
            _classificationId = GetRequiredTokenId(ClassificationToken);
            _separatorId = GetRequiredTokenId(SeparatorToken);
            _paddingId = GetRequiredTokenId(PaddingToken);
        }

        public EncodedInput Encode(string text, int maxLength)
        {
            var tokenIds = new List<int> { _classificationId };
            var normalizedText = _lowercase ? text.ToLowerInvariant() : text;
            foreach (var word in WordRegex.Matches(normalizedText).Cast<Match>().Select(match => match.Value))
            {
                foreach (var tokenId in TokenizeWord(word))
                {
                    if (tokenIds.Count >= maxLength - 1)
                    {
                        break;
                    }

                    tokenIds.Add(tokenId);
                }

                if (tokenIds.Count >= maxLength - 1)
                {
                    break;
                }
            }

            tokenIds.Add(_separatorId);

            var inputIds = Enumerable.Repeat((long)_paddingId, maxLength).ToArray();
            var attentionMask = new long[maxLength];
            var tokenTypeIds = new long[maxLength];
            for (var i = 0; i < Math.Min(tokenIds.Count, maxLength); i++)
            {
                inputIds[i] = tokenIds[i];
                attentionMask[i] = 1L;
            }

            return new EncodedInput(inputIds, attentionMask, tokenTypeIds);
        }

        private IEnumerable<int> TokenizeWord(string word)
        {
            if (_vocabulary.TryGetValue(word, out var id))
            {
                yield return id;
                yield break;
            }

            var start = 0;
            var subTokens = new List<int>();
            while (start < word.Length)
            {
                var end = word.Length;
                int? currentId = null;
                while (start < end)
                {
                    var piece = word[start..end];
                    if (start > 0)
                    {
                        piece = "##" + piece;
                    }

                    if (_vocabulary.TryGetValue(piece, out var pieceId))
                    {
                        currentId = pieceId;
                        break;
                    }

                    end--;
                }

                if (currentId is null)
                {
                    yield return _unknownId;
                    yield break;
                }

                subTokens.Add(currentId.Value);
                start = end;
            }

            foreach (var tokenId in subTokens)
            {
                yield return tokenId;
            }
        }

        private int GetRequiredTokenId(string token)
        {
            return _vocabulary.TryGetValue(token, out var id)
                ? id
                : throw new InvalidOperationException($"Tokenizer vocabulary is missing required token '{token}'.");
        }
    }

    private sealed record EncodedInput(long[] InputIds, long[] AttentionMask, long[] TokenTypeIds);
}
