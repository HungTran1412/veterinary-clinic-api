using System.ComponentModel.DataAnnotations;

namespace VeterinaryClinic.Shared
{
    public enum PermissionVeterinaryClinicEnum
    {
        #region Phiếu đánh giá

        [Display(GroupName = "Phiếu đánh giá", Name = "Thêm phiếu đánh giá")]
        PHIEU_DANH_GIA_ADD,

        [Display(GroupName = "Phiếu đánh giá", Name = "Sửa phiếu đánh giá")]
        PHIEU_DANH_GIA_EDIT,

        [Display(GroupName = "Phiếu đánh giá", Name = "Xóa phiếu đánh giá")]
        PHIEU_DANH_GIA_DELETE,

        [Display(GroupName = "Phiếu đánh giá", Name = "Xem thông tin phiếu đánh giá")]
        PHIEU_DANH_GIA_VIEW,

        #endregion
        
        #region Quản lý dịch vụ

        [Display(GroupName = "Quản lý dịch vụ", Name = "Thêm dịch vụ")]
        SERVICE_MANAGER_ADD,

        [Display(GroupName = "Quản lý dịch vụ", Name = "Sửa dịch vụ")]
        SERVICE_MANAGER_EDIT,

        [Display(GroupName = "Quản lý dịch vụ", Name = "Xóa dịch vụ")]
        SERVICE_MANAGER_DELETE,

        [Display(GroupName = "Quản lý dịch vụ", Name = "Xem thông tin dịch vụ")]
        SERVICE_MANAGER_VIEW,

        #endregion
        
        #region Quản lý người dùng

        [Display(GroupName = "Quản lý người dùng", Name = "Thêm người dùng")]
        USER_MANAGER_ADD,

        [Display(GroupName = "Quản lý người dùng", Name = "Sửa người dùng")]
        USER_MANAGER_EDIT,

        [Display(GroupName = "Quản lý người dùng", Name = "Xóa người dùng")]
        USER_MANAGER_DELETE,

        [Display(GroupName = "Quản lý người dùng", Name = "Xem thông tin người dùng")]
        USER_MANAGER_VIEW,

        #endregion
        
        #region Quản lý chuyên ngành

        [Display(GroupName = "Quản lý chuyên ngành", Name = "Thêm chuyên ngành")]
        SPECIALIZATION_MANAGER_ADD,

        [Display(GroupName = "Quản lý chuyên ngành", Name = "Sửa chuyên ngành")]
        SPECIALIZATION_MANAGER_EDIT,

        [Display(GroupName = "Quản lý chuyên ngành", Name = "Xóa chuyên ngành")]
        SPECIALIZATION_MANAGER_DELETE,

        [Display(GroupName = "Quản lý chuyên ngành", Name = "Xem thông tin chuyên ngành")]
        SPECIALIZATION_MANAGER_VIEW,

        #endregion
    }   
}