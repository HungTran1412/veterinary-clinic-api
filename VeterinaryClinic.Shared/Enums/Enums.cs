namespace VeterinaryClinic.Shared
{
    public enum Role
    {
        ADMIN,
        DOCTOR,
        RECEPTIONIST,
        CUSTOMER,
    }

    public enum AppointmentStatus
    {
        PENDING_CONFIRMATION,
        CONFIRMED,
        REJECTED,
        CANCELLED,
        CANCELLATION_REQUESTED,
        IN_PROGRESS,
        PAYMENT_PENDING,
        COMPLETED,
        NO_SHOW
    }

    public enum AppointmentAction
    {
        CONFIRM,
        REJECT,
        CUSTOMER_CANCEL,
        START_CONSULTATION,
        REQUEST_CANCELLATION,
        APPROVE_CANCELLATION,
        REJECT_CANCELLATION_REQUEST,
        MARK_NO_SHOW,
        COMPLETE_CONSULTATION,
        COMPLETE_PAYMENT
    }
}
