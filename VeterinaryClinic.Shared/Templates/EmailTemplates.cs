namespace VeterinaryClinic.Shared.Templates
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
    }
}
