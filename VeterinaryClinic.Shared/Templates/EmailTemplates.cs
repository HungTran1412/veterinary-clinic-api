namespace VeterinaryClinic.Shared
{
    public static class EmailTemplates
    {
        /// <summary>
        /// Template email thông báo cấp tài khoản mới
        /// </summary>
        /// <param name="fullName">Họ tên người nhận</param>
        /// <param name="username">Tên đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <param name="role">Vai trò</param>
        /// <returns>Nội dung HTML của email</returns>
        public static string GetAccountCreatedEmail(string fullName, string username, string password, string role)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                    <h3 style='color: #0056b3;'>Xin chào {fullName},</h3>
                    <p>Bạn đã được cấp tài khoản truy cập hệ thống quản lý phòng khám thú y.</p>
                    <div style='background-color: #f9f9f9; padding: 15px; border-left: 4px solid #0056b3; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Thông tin đăng nhập của bạn:</strong></p>
                        <ul style='margin-top: 5px;'>
                            <li><strong>Tên đăng nhập:</strong> {username}</li>
                            <li><strong>Mật khẩu:</strong> {password}</li>
                            <li><strong>Vai trò:</strong> {role}</li>
                        </ul>
                    </div>
                    <p>Vui lòng đăng nhập và đổi mật khẩu ngay trong lần đăng nhập đầu tiên để bảo mật tài khoản.</p>
                    <br/>
                    <hr style='border: none; border-top: 1px solid #eee;' />
                    <p style='font-size: 0.9em; color: #777;'>
                        Trân trọng,<br/>
                        <strong>Ban quản trị hệ thống Veterinary Clinic</strong>
                    </p>
                </div>
            ";
        }

        /// <summary>
        /// Template email xác thực tài khoản
        /// </summary>
        /// <param name="fullName">Họ tên người nhận</param>
        /// <param name="verificationLink">Link để xác thực</param>
        /// <returns>Nội dung HTML của email</returns>
        public static string GetVerificationEmail(string fullName, string verificationLink)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #0056b3; text-align: center;'>Xác thực tài khoản của bạn</h2>
                    <p>Xin chào {fullName},</p>
                    <p>Cảm ơn bạn đã đăng ký tài khoản tại Veterinary Clinic. Vui lòng nhấp vào nút bên dưới để hoàn tất quá trình đăng ký và kích hoạt tài khoản của bạn.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verificationLink}' style='background-color: #28a745; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-size: 1.1em;'>Kích hoạt tài khoản</a>
                    </div>
                    <p>Nếu nút trên không hoạt động, bạn cũng có thể sao chép và dán đường dẫn sau vào trình duyệt của mình:</p>
                    <p style='word-break: break-all; font-size: 0.9em;'><a href='{verificationLink}'>{verificationLink}</a></p>
                    <p>Lưu ý: Đường dẫn này sẽ hết hạn sau 24 giờ.</p>
                    <br/>
                    <hr style='border: none; border-top: 1px solid #eee;' />
                    <p style='font-size: 0.9em; color: #777;'>
                        Trân trọng,<br/>
                        <strong>Ban quản trị hệ thống Veterinary Clinic</strong>
                    </p>
                </div>
            ";
        }
    }
}
