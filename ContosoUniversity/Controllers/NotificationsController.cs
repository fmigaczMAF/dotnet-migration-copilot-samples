using System;
using System.Web.Mvc;

namespace ContosoUniversity.Controllers
{
    public class NotificationsController : BaseController
    {
        private const int MaxNotificationsPerPoll = 10;

        // GET: /Notifications/GetNotifications - Peek pending notifications.
        // Messages are NOT removed from the queue here; clients must call
        // MarkAsRead with the returned MessageId once the notification has
        // been delivered to the user.
        [HttpGet]
        public JsonResult GetNotifications()
        {
            try
            {
                var notifications = notificationService.PeekNotifications(MaxNotificationsPerPoll);
                return Json(new
                {
                    success = true,
                    notifications,
                    count = notifications.Count
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving notifications: {ex.Message}");
                return Json(new { success = false, message = "Error retrieving notifications" }, JsonRequestBehavior.AllowGet);
            }
        }

        // POST: /Notifications/MarkAsRead - Acknowledge (remove) a queued notification.
        [HttpPost]
        public JsonResult MarkAsRead(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                return Json(new { success = false, message = "messageId is required" });
            }

            try
            {
                var removed = notificationService.MarkAsRead(messageId);
                return Json(new { success = removed });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error marking notification as read: {ex.Message}");
                return Json(new { success = false, message = "Error updating notification" });
            }
        }

        // GET: Notifications/Index - Admin notification dashboard
        public ActionResult Index()
        {
            return View();
        }
    }
}
