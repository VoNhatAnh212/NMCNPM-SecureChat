# SecureChat Authentication Test Cases


<div style="color:#00B050">
  <p>Written by: 23120212-Võ Nhật Anh</p>
  <p>Edited by:</p>
  <p>Reviewed by:</p>
</div>

## 1.2.1 AUTH_01 - Kết Nối Tới Server Có TLS Certificate Hợp Lệ

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Client kết nối tới SecureChat Server qua HTTPS/TLS với certificate hợp lệ, hostname đúng và được ký bởi SecureChat CA.</td></tr>
    <tr><td>Input Data</td><td>API base URL HTTPS; server certificate hợp lệ; client có file CA tại cấu hình <code>ServerCaCertificateFile</code>.</td></tr>
    <tr><td>Expected Output</td><td>Client xác thực certificate thành công và tiếp tục gửi request tới server.</td></tr>
    <tr><td>Test steps</td><td>1. Khởi động server với certificate hợp lệ.<br>2. Khởi động client.<br>3. Thực hiện request đăng nhập hoặc gọi API qua HTTPS.<br>4. Quan sát kết nối TLS.</td></tr>
    <tr><td>Actual Output</td><td>Trả về <code>true</code> và request được gửi tiếp.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.2 AUTH_02 - Kết Nối Tới Server Có Certificate Hết Hạn

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Server dùng TLS certificate đã hết hạn.</td></tr>
    <tr><td>Input Data</td><td>Server certificate có thời hạn kết thúc nhỏ hơn ngày hiện tại.</td></tr>
    <tr><td>Expected Output</td><td>Client từ chối kết nối và không gửi credential/token tới server.</td></tr>
    <tr><td>Test steps</td><td>1. Cấu hình server dùng certificate hết hạn.<br>2. Khởi động server.<br>3. Khởi động client và thử đăng nhập.<br>4. Kiểm tra request bị chặn.</td></tr>
    <tr><td>Actual Output</td><td>Certificate hết hạn sẽ làm custom chain build thất bại, validator trả về <code>false</code>, HTTPS request thất bại.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.3 AUTH_03 - Kết Nối Tới Server Có Certificate Không Đáng Tin Cậy

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Server certificate không được ký bởi SecureChat CA mà client tin cậy.</td></tr>
    <tr><td>Input Data</td><td>Certificate tự ký hoặc được ký bởi CA khác với <code>ca.cert</code> của client.</td></tr>
    <tr><td>Expected Output</td><td>Client từ chối kết nối TLS.</td></tr>
    <tr><td>Test steps</td><td>1. Cấu hình server dùng certificate không thuộc SecureChat CA.<br>2. Khởi động client.<br>3. Thử đăng nhập hoặc gọi API.<br>4. Quan sát lỗi kết nối.</td></tr>
    <tr><td>Actual Output</td><td>Client chỉ trust CA được load từ <code>ServerCaCertificateFile</code>. Nếu chain không build được với CA này hoặc file CA thiếu, validator trả về <code>false</code>.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.4 AUTH_04 - Kết Nối Tới Server Có Hostname Không Khớp Certificate

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Client kết nối bằng hostname không khớp CN/SAN trong server certificate.</td></tr>
    <tr><td>Input Data</td><td>Certificate cấp cho hostname A nhưng client kết nối bằng hostname B.</td></tr>
    <tr><td>Expected Output</td><td>Client từ chối kết nối do hostname mismatch.</td></tr>
    <tr><td>Test steps</td><td>1. Chạy server với certificate cho hostname cụ thể.<br>2. Đổi client kết nối bằng hostname khác.<br>3. Thử gọi API.<br>4. Kiểm tra kết nối bị chặn.</td></tr>
    <tr><td>Actual Output</td><td>Nếu có lỗi này, hàm trả về <code>false</code> ngay cả khi CA hợp lệ.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.5 AUTH_05 - Đăng Ký Tài Khoản Với Thông Tin Hợp Lệ

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng đăng ký account mới bằng email chưa tồn tại, display name hợp lệ và password tối thiểu 8 ký tự.</td></tr>
    <tr><td>Input Data</td><td>Email chưa đăng ký; display name không trống; password hợp lệ; OTP đúng; device name hợp lệ.</td></tr>
    <tr><td>Expected Output</td><td>Server gửi OTP, xác thực OTP thành công, tạo user, tạo device, lưu public key và cấp access/refresh token.</td></tr>
    <tr><td>Test steps</td><td>1. Mở RegisterForm.<br>2. Nhập email, display name, password hợp lệ.<br>3. Nhấn gửi OTP.<br>4. Nhập OTP 6 số nhận qua email.<br>5. Hoàn tất đăng ký thiết bị.</td></tr>
    <tr><td>Actual Output</td><td>Sau khi OTP đúng, client generate RSA key pair, gọi complete-registration, lưu private key/tokens local và mở MainForm.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.6 AUTH_06 - Đăng Ký Với Tên Đăng Nhập Trống

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Trong project hiện tại, username đăng nhập chính là email; RegisterForm dùng display name thay cho tên hiển thị.</td></tr>
    <tr><td>Input Data</td><td>Email/username trống hoặc display name trống; password hợp lệ.</td></tr>
    <tr><td>Expected Output</td><td>Client không gửi request đăng ký và báo lỗi thông tin không hợp lệ.</td></tr>
    <tr><td>Test steps</td><td>1. Mở RegisterForm.<br>2. Để trống email hoặc display name.<br>3. Nhập password hợp lệ.<br>4. Nhấn gửi OTP.</td></tr>
    <tr><td>Actual Output</td><td>RegisterForm hiển thị: <code>Enter a valid email, display name, and password of at least 8 characters.</code></td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.7 AUTH_07 - Đăng Ký Với Email Sai Định Dạng

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng nhập email không đúng định dạng khi đăng ký tài khoản.</td></tr>
    <tr><td>Input Data</td><td>Email sai định dạng, ví dụ <code>abc</code>, <code>abc@</code>, <code>abc.com</code>; display name và password hợp lệ.</td></tr>
    <tr><td>Expected Output</td><td>Hệ thống từ chối đăng ký, không gửi OTP và hiển thị thông báo yêu cầu nhập email hợp lệ.</td></tr>
    <tr><td>Test steps</td><td>1. Mở RegisterForm.<br>2. Nhập email sai định dạng.<br>3. Nhập display name hợp lệ.<br>4. Nhập password hợp lệ.<br>5. Nhấn gửi OTP.</td></tr>
    <tr><td>Actual Output</td><td>Client phát hiện email sai định dạng, không gửi request đăng ký đến server và hiển thị thông báo yêu cầu nhập email hợp lệ.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.8 AUTH_08 - Đăng Ký Với Username Đã Tồn Tại

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Project hiện tại chưa có username riêng; email là định danh đăng nhập duy nhất.</td></tr>
    <tr><td>Input Data</td><td>Email đã tồn tại; display name bất kỳ; password hợp lệ.</td></tr>
    <tr><td>Expected Output</td><td>Server từ chối đăng ký và không gửi OTP cho email đã có account.</td></tr>
    <tr><td>Test steps</td><td>1. Đảm bảo database đã có account với email cần test.<br>2. Mở RegisterForm.<br>3. Nhập lại email đó, display name và password hợp lệ.<br>4. Nhấn gửi OTP.</td></tr>
    <tr><td>Actual Output</td><td>Nếu email đã tồn tại, server trả lỗi <code>Email is already registered.</code> với status 409; client hiển thị <code>This email already has an account. Please log in instead.</code></td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.9 AUTH_09 - Đăng Ký Với Mật Khẩu Quá Ngắn

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng nhập password ngắn hơn 8 ký tự.</td></tr>
    <tr><td>Input Data</td><td>Email hợp lệ; display name hợp lệ; password như <code>123</code>.</td></tr>
    <tr><td>Expected Output</td><td>Client không gửi request đăng ký và báo lỗi password quá ngắn.</td></tr>
    <tr><td>Test steps</td><td>1. Mở RegisterForm.<br>2. Nhập email và display name hợp lệ.<br>3. Nhập password dưới 8 ký tự.<br>4. Nhấn gửi OTP.</td></tr>
    <tr><td>Actual Output</td><td>RegisterForm chặn ngay tại client và hiển thị: <code>Enter a valid email, display name, and password of at least 8 characters.</code></td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.10 AUTH_10 - Đăng Nhập Với Thông Tin Hợp Lệ

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng đăng nhập bằng account hợp lệ trên thiết bị đã trust hoặc bằng Login with other account.</td></tr>
    <tr><td>Input Data</td><td>Email/username hợp lệ; password đúng; device private key đúng hoặc OTP đúng cho thiết bị mới.</td></tr>
    <tr><td>Expected Output</td><td>Đăng nhập thành công, server cấp access token và refresh token, client mở màn hình chính.</td></tr>
    <tr><td>Test steps</td><td>1. Mở LoginForm.<br>2. Chọn account local đã lưu hoặc chọn Login with other account.<br>3. Nhập thông tin hợp lệ.<br>4. Hoàn tất challenge/OTP nếu cần.<br>5. Quan sát màn hình chính.</td></tr>
    <tr><td>Actual Output</td><td>Với saved account, client lấy challenge, ký bằng private key local, server verify chữ ký. Với thiết bị mới, server gửi OTP rồi đăng ký public key thiết bị. Client lưu token bằng DPAPI và mở MainForm.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.11 AUTH_11 - Đăng Nhập Với Mật Khẩu Sai

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng nhập account tồn tại nhưng password sai.</td></tr>
    <tr><td>Input Data</td><td>Email/username tồn tại; password sai.</td></tr>
    <tr><td>Expected Output</td><td>Server từ chối đăng nhập và không cấp token.</td></tr>
    <tr><td>Test steps</td><td>1. Chọn Login with other account.<br>2. Nhập email tồn tại.<br>3. Nhập password sai.<br>4. Nhấn Continue.</td></tr>
    <tr><td>Actual Output</td><td>Server trả Unauthorized với message <code>Invalid username or password.</code>. Client bắt exception và hiển thị message này trên LoginForm.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.12 AUTH_12 - Đăng Nhập Với Tài Khoản Không Tồn Tại

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng nhập email/username không có trong database.</td></tr>
    <tr><td>Input Data</td><td>Email/username không tồn tại; password bất kỳ.</td></tr>
    <tr><td>Expected Output</td><td>Server từ chối đăng nhập và không tiết lộ account có tồn tại hay không.</td></tr>
    <tr><td>Test steps</td><td>1. Chọn Login with other account.<br>2. Nhập email không tồn tại.<br>3. Nhập password.<br>4. Nhấn Continue.</td></tr>
    <tr><td>Actual Output</td><td> Dùng cùng message với mật khẩu sai: <code>Invalid username or password.</code>. Không cấp access token/refresh token.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.13 AUTH_13 - Đăng Nhập Khi Bỏ Trống Thông Tin

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng bỏ trống account hoặc password khi đăng nhập.</td></tr>
    <tr><td>Input Data</td><td>Email/username trống hoặc password trống.</td></tr>
    <tr><td>Expected Output</td><td>Client chặn thao tác và yêu cầu nhập đầy đủ thông tin.</td></tr>
    <tr><td>Test steps</td><td>1. Mở LoginForm.<br>2. Chọn Login with other account.<br>3. Để trống email/username hoặc password.<br>4. Nhấn Continue.</td></tr>
    <tr><td>Actual Output</td><td>LoginForm hiển thị: <code>Enter email/username and password.</code>. Nếu đăng nhập bằng danh sách local mà chưa chọn account, hiển thị <code>Select an account before signing in.</code></td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.14 AUTH_14 - Truy Cập Màn Hình Chính Sau Khi Đăng Nhập Thành Công

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Người dùng đã đăng nhập thành công.</td></tr>
    <tr><td>Input Data</td><td>Login response có access token, refresh token, expiresAt và user.</td></tr>
    <tr><td>Expected Output</td><td>Client đóng/ẩn LoginForm và mở MainForm.</td></tr>
    <tr><td>Test steps</td><td>1. Đăng nhập thành công.<br>2. Chờ client xử lý login response.<br>3. Quan sát form được mở.</td></tr>
    <tr><td>Actual Output</td><td>RegisterForm sau đăng ký thành công cũng mở MainForm theo cùng hướng.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.15 AUTH_15 - Truy Cập Chức Năng Yêu Cầu Đăng Nhập Khi Chưa Đăng Nhập

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Client không có access token nhưng gọi API yêu cầu xác thực.</td></tr>
    <tr><td>Input Data</td><td>Không có Authorization Bearer token hoặc token trống.</td></tr>
    <tr><td>Expected Output</td><td>Server trả Unauthorized; client không hiển thị dữ liệu bảo vệ.</td></tr>
    <tr><td>Test steps</td><td>1. Xóa token local hoặc logout.<br>2. Gọi API bảo vệ như friends/chats/devices.<br>3. Quan sát phản hồi.</td></tr>
    <tr><td>Actual Output</td><td><code>ApiClient</code> chỉ gắn Authorization header khi có access token. Với endpoint có <code>[Authorize]</code>, server trả 401; nếu refresh token không có hoặc refresh thất bại, request retry vẫn không thành công.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.16 AUTH_16 - Phiên Đăng Nhập Hết Hạn

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Access token hết hạn trong lúc user đang dùng app.</td></tr>
    <tr><td>Input Data</td><td>Access token hết hạn; refresh token còn active hoặc đã hết hạn/revoked.</td></tr>
    <tr><td>Expected Output</td><td>Nếu refresh token còn hạn, client tự refresh token và retry request. Nếu refresh token không hợp lệ, user cần đăng nhập lại.</td></tr>
    <tr><td>Test steps</td><td>1. Đăng nhập thành công.<br>2. Làm access token hết hạn.<br>3. Gọi API bảo vệ.<br>4. Kiểm tra refresh token và retry request.</td></tr>
    <tr><td>Actual Output</td><td>Server revoke refresh token cũ, phát access/refresh token mới; client lưu token mới bằng DPAPI và retry request.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.17 AUTH_17 - Đăng Xuất Khỏi Hệ Thống

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>User đang đăng nhập và nhấn Logout.</td></tr>
    <tr><td>Input Data</td><td>Session hợp lệ; refresh token active.</td></tr>
    <tr><td>Expected Output</td><td>Server revoke refresh tokens của device, client xóa token/session local và quay về LoginForm.</td></tr>
    <tr><td>Test steps</td><td>1. Đăng nhập thành công.<br>2. Nhấn Logout.<br>3. Xác nhận nếu có dialog.<br>4. Kiểm tra session local và màn hình.</td></tr>
    <tr><td>Actual Output</td><td>Client gửi yêu cầu đăng xuất đến server. Server xử lý đăng xuất cho thiết bị hiện tại, sau đó client xóa dữ liệu phiên đăng nhập local và quay về màn hình Login. MainForm chuyển về LoginForm.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.18 AUTH_18 - Truy Cập Lại Sau Khi Đăng Xuất

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>User đã logout nhưng thử truy cập lại chức năng yêu cầu đăng nhập.</td></tr>
    <tr><td>Input Data</td><td>Token local đã xóa; refresh token của device đã revoke trên server.</td></tr>
    <tr><td>Expected Output</td><td>Client không thể gọi API bảo vệ bằng session cũ; user phải đăng nhập lại.</td></tr>
    <tr><td>Test steps</td><td>1. Đăng nhập.<br>2. Logout.<br>3. Thử gọi API bảo vệ hoặc mở lại app.<br>4. Quan sát yêu cầu đăng nhập lại.</td></tr>
    <tr><td>Actual Output</td><td>Sau logout, <code>AuthSessionService</code> không còn access/refresh token và token file local bị xóa. Nếu gọi API bảo vệ, server trả 401; nếu mở app lại, LoginForm hiển thị account local hoặc trạng thái chưa đăng nhập nhưng không có token active.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.19 AUTH_19 - Xác Thực RSA Challenge Thành Công

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Device đã đăng ký public key trên server và client còn private key tương ứng.</td></tr>
    <tr><td>Input Data</td><td>DeviceId hợp lệ; challenge do server tạo; chữ ký được tạo bằng private key đúng.</td></tr>
    <tr><td>Expected Output</td><td>Server verify chữ ký thành công và cấp access/refresh token.</td></tr>
    <tr><td>Test steps</td><td>1. Chọn account local đã lưu.<br>2. Client gọi endpoint lấy challenge.<br>3. Client ký challenge bằng private key local.<br>4. Client gửi signed challenge lên server.<br>5. Quan sát kết quả login.</td></tr>
    <tr><td>Actual Output</td><td>Client yêu cầu challenge từ server và ký challenge bằng private key của thiết bị. Server kiểm tra chữ ký bằng public key đã lưu của thiết bị; nếu hợp lệ, server xác nhận đăng nhập thành công và trả thông tin phiên đăng nhập cho client.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 1.2.20 AUTH_20 - Xác Thực RSA Challenge Thất Bại

<table border="1" cellpadding="8" cellspacing="0" style="width:100%">
  <tbody>
    <tr><th colspan="2" style="text-align:left">Test case</th></tr>
    <tr><td>Related feature</td><td>Authentication &amp; Account Management</td></tr>
    <tr><td>Context</td><td>Client gửi chữ ký sai, dùng private key không khớp hoặc challenge đã bị chỉnh sửa/hết hạn.</td></tr>
    <tr><td>Input Data</td><td>DeviceId hợp lệ; signedChallenge không verify được bằng public key active của device.</td></tr>
    <tr><td>Expected Output</td><td>Server từ chối login và không cấp token.</td></tr>
    <tr><td>Test steps</td><td>1. Lấy challenge hợp lệ.<br>2. Ký bằng private key sai hoặc sửa chữ ký.<br>3. Gửi request login.<br>4. Quan sát phản hồi server.</td></tr>
    <tr><td>Actual Output</td><td>Nếu xác thực chữ ký thất bại, server từ chối đăng nhập và không cấp phiên đăng nhập. Client nhận lỗi từ server và hiển thị thông báo đăng nhập thất bại cho người dùng.</td></tr>
    <tr><td>Result</td><td>Passed</td></tr>
  </tbody>
</table>

## 7. AI Usage Declaration

- **ChatGPT.** GPT-5.3-mini, OpenAI, chat.openai.com, accessed 8:12 on May 5, 2026, prompt: "hãy liệt kê và mô tả chi tiết các test case quan trọng của feature Authentication & Account Management trong hệ thống SecureChat.", được tham khảo cho phần Authentication Test Cases; AI đã liệt kê các test case quan trọng và mô tả chi tiết các thành phần như Related feature, Context, Input Data, Expected Output, Test steps, Actual Output và Result; do mô tả của AI chưa đảm bảo hoàn toàn đúng với project hiện tại, thiếu tính bảo mật và còn thiếu một số chi tiết triển khai nên em đã chỉnh sửa lại dựa trên code thực tế của hệ thống SecureChat, và AI chỉ giúp em tiết kiệm thời gian cho việc phải tự ngồi viết chi tiết từng test cases.
