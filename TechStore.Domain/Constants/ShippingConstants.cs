namespace TechStore.Domain.Constants
{
    public static class ShippingConstants
    {
        // ── Order Statuses ──
        public const string StatusDelivering = "Shipped";
        public const string StatusPending = "Pending";
        public const string StatusCompleted = "Completed";
        public const string StatusCancelled = "Cancelled";
        public const string StatusDelivered = "Delivered";

        // ── Configuration Default Values ──
        public const double DefaultStoreLatitude = 10.841122;
        public const double DefaultStoreLongitude = 106.809935;
        public const decimal DefaultBaseShippingFee = 15000m;
        public const double DefaultBaseDistanceKm = 2.0;
        public const decimal DefaultPerKmShippingFee = 5000m;

        // ── Error & Success Messages ──
        public const string InvalidRequest = "Yêu cầu không hợp lệ.";
        public const string InvalidDestinationCoordinates = "Toạ độ đích đến phải khác 0.";
        public const string OrderNotFound = "Đơn hàng không tồn tại.";
        public const string OrderNotDelivering = "Đơn hàng chưa ở trong trạng thái giao hoặc chưa được gán shipper.";
        public const string ShipperLocationNotFound = "Shipper hiện tại chưa cập nhật toạ độ GPS.";
        public const string UpdateLocationSuccess = "Cập nhật toạ độ và phát realtime thành công.";
        public const string CalculateShippingFailed = "Lỗi khi tính toán phí vận chuyển: {0}";
        public const string RealtimeTrackingFailed = "Lỗi hệ thống khi tính phí vận chuyển: {0}";
        public const string TrackingDataInvalid = "Dữ liệu định vị không hợp lệ.";
        public const string LocationOutsideServiceArea = "Toạ độ GPS nằm ngoài khu vực phục vụ (Việt Nam) — kiểm tra lại vị trí thiết bị.";

        // ── Phạm vi toạ độ hợp lệ (Việt Nam) — chặn GPS mặc định của máy ảo
        //    (vd (0,0) hoặc Mountain View 37.42,-122.08) lọt vào hệ thống. ──
        public const double VietnamMinLatitude = 8.0;
        public const double VietnamMaxLatitude = 24.0;
        public const double VietnamMinLongitude = 102.0;
        public const double VietnamMaxLongitude = 110.0;
        public const string ImageFileRequired = "Vui lòng chọn ảnh chụp xác nhận.";
        public const string UploadProofFailed = "Lỗi khi upload ảnh và hoàn tất đơn hàng: {0}";
    }
}
