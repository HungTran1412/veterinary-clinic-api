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

        /// <summary>
        /// Template email thông báo đăng ký thành công
        /// </summary>
        /// <param name="fullName">Họ tên người dùng</param>
        /// <param name="loginUrl">URL trang đăng nhập</param>
        /// <returns>Nội dung HTML của email</returns>
        public static string GetRegistrationSuccessEmail(string fullName, string loginUrl)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #28a745; text-align: center;'>Đăng ký tài khoản thành công!</h2>
                    <p>Xin chào {fullName},</p>
                    <p>Chúc mừng bạn đã kích hoạt thành công tài khoản tại Veterinary Clinic.</p>
                    <p>Bây giờ bạn có thể đăng nhập vào hệ thống và bắt đầu sử dụng các dịch vụ của chúng tôi.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{loginUrl}' style='background-color: #007bff; color: white; padding: 15px 25px; text-decoration: none; border-radius: 5px; font-size: 1.1em;'>Đăng nhập ngay</a>
                    </div>
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
        /// Template email xác nhận lịch hẹn cho khách hàng
        /// </summary>
        public static string GetAppointmentConfirmationEmailForCustomer(
            string customerName,
            string petName,
            string serviceName,
            string appointmentDate,
            string startTime,
            string endTime,
            string doctorName,
            string appointmentCode)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #0056b3; text-align: center;'>Xác nhận lịch hẹn của bạn</h2>
                    <p>Xin chào {customerName},</p>
                    <p>Lịch hẹn của bạn tại Veterinary Clinic đã được xác nhận thành công với các chi tiết sau:</p>
                    <div style='background-color: #f9f9f9; padding: 15px; border-left: 4px solid #0056b3; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Mã lịch hẹn:</strong> {appointmentCode}</p>
                        <p style='margin: 0;'><strong>Thú cưng:</strong> {petName}</p>
                        <p style='margin: 0;'><strong>Dịch vụ:</strong> {serviceName}</p>
                        <p style='margin: 0;'><strong>Ngày hẹn:</strong> {appointmentDate}</p>
                        <p style='margin: 0;'><strong>Thời gian:</strong> {startTime} - {endTime}</p>
                        <p style='margin: 0;'><strong>Bác sĩ phụ trách:</strong> {doctorName}</p>
                    </div>
                    <p>Vui lòng đến đúng giờ để đảm bảo thú cưng của bạn được phục vụ tốt nhất.</p>
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
        /// Template email thông báo lịch hẹn mới cho bác sĩ
        /// </summary>
        public static string GetAppointmentConfirmationEmailForDoctor(
            string doctorName,
            string customerName,
            string petName,
            string serviceName,
            string appointmentDate,
            string startTime,
            string endTime,
            string appointmentCode)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #0056b3; text-align: center;'>Bạn có lịch hẹn mới</h2>
                    <p>Xin chào Bác sĩ {doctorName},</p>
                    <p>Bạn có một lịch hẹn mới được tạo với các chi tiết sau:</p>
                    <div style='background-color: #f9f9f9; padding: 15px; border-left: 4px solid #0056b3; margin: 20px 0;'>
                        <p style='margin: 0;'><strong>Mã lịch hẹn:</strong> {appointmentCode}</p>
                        <p style='margin: 0;'><strong>Khách hàng:</strong> {customerName}</p>
                        <p style='margin: 0;'><strong>Thú cưng:</strong> {petName}</p>
                        <p style='margin: 0;'><strong>Dịch vụ:</strong> {serviceName}</p>
                        <p style='margin: 0;'><strong>Ngày hẹn:</strong> {appointmentDate}</p>
                        <p style='margin: 0;'><strong>Thời gian:</strong> {startTime} - {endTime}</p>
                    </div>
                    <p>Vui lòng kiểm tra lịch làm việc của bạn.</p>
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
        /// Template email gửi mã OTP
        /// </summary>
        /// <param name="fullName">Họ tên người nhận</param>
        /// <param name="otp">Mã OTP</param>
        /// <returns>Nội dung HTML của email</returns>
        public static string GetOtpEmail(string fullName, string otp)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: auto; border: 1px solid #ddd; padding: 20px;'>
                    <h2 style='color: #0056b3; text-align: center;'>Mã OTP để đặt lại mật khẩu</h2>
                    <p>Xin chào {fullName},</p>
                    <p>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP dưới đây để hoàn tất quá trình.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <p style='font-size: 2em; font-weight: bold; letter-spacing: 5px; color: #28a745; border: 2px dashed #28a745; padding: 10px; display: inline-block;'>{otp}</p>
                    </div>
                    <p>Mã OTP này sẽ hết hạn sau 5 phút. Vui lòng không chia sẻ mã này với bất kỳ ai.</p>
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
