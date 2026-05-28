namespace VeterinaryClinic.Shared
{
    /// <summary>
    /// Vai trò
    /// </summary>
    public enum Role
    {
        ADMIN,
        DOCTOR,
        RECEPTIONIST,
        CUSTOMER,
    }

    /// <summary>
    /// Trạng thái quy trình
    /// </summary>
    public enum AppointmentStatus
    {
        PENDING_CONFIRMATION,
        CONFIRMED,
        REJECTED,
        CANCELLED,
        IN_PROGRESS,
        PAYMENT_PENDING,
        COMPLETED,
        NO_SHOW
    }

    /// <summary>
    /// Hành động của quy trình
    /// </summary>
    public enum AppointmentAction
    {
        CONFIRM,
        REJECT,
        CUSTOMER_CANCEL,
        START_CONSULTATION,
        MARK_NO_SHOW,
        COMPLETE_CONSULTATION,
        COMPLETE_PAYMENT,
        CASH_PAYMENT,
        BANK_TRANSFER
    }

    /// <summary>
    /// Loại thông báo
    /// </summary>
    public enum NotificationType
    {
        MESSAGE
    }

    /// <summary>
    /// Loại dữ liệu liên quan đến thông báo
    /// </summary>
    public enum RelatedEntityType
    {
        Appointment,
        User
    }

    /// <summary>
    /// Token xác thực
    /// </summary>
    public enum TokenType
    {
        OTP,
        VERIFY
    }

    /// <summary>
    /// Phương thức thanh toán
    /// </summary>
    public enum PaymentMethod
    {
        CASH,
        VNPAY
    }

    /// <summary>
    /// Trạng thái thanh toán
    /// </summary>
    public enum PaymentStatus
    {
        PENDING,
        SUCCESS,
        FAILED
    }

    /// <summary>
    /// Trạng thái đăng ký lịch làm việc
    /// </summary>
    public enum WorkScheduleRegisterStatus
    {
        PENDING, 
        APPROVED,
        REJECTED,
        CANCELED
    }
}
