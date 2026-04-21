using System;
using System.Collections.Generic;
using System.Messaging;
using System.Configuration;
using ContosoUniversity.Models;
using Newtonsoft.Json;

namespace ContosoUniversity.Services
{
    public class NotificationService : IDisposable
    {
        private readonly string _queuePath;
        private readonly MessageQueue _queue;
        private bool _disposed;

        public NotificationService()
        {
            // Get queue path from configuration or use default
            _queuePath = ConfigurationManager.AppSettings["NotificationQueuePath"] ?? @".\Private$\ContosoUniversityNotifications";

            // Ensure the queue exists
            if (!MessageQueue.Exists(_queuePath))
            {
                _queue = MessageQueue.Create(_queuePath);
                _queue.SetPermissions("Everyone", MessageQueueAccessRights.FullControl);
            }
            else
            {
                _queue = new MessageQueue(_queuePath);
            }

            // Configure queue formatter
            _queue.Formatter = new XmlMessageFormatter(new Type[] { typeof(string) });
            _queue.MessageReadPropertyFilter.SetAll();
        }

        public void SendNotification(string entityType, string entityId, EntityOperation operation, string userName = null)
        {
            SendNotification(entityType, entityId, null, operation, userName);
        }

        public void SendNotification(string entityType, string entityId, string entityDisplayName, EntityOperation operation, string userName = null)
        {
            try
            {
                var notification = new Notification
                {
                    EntityType = entityType,
                    EntityId = entityId,
                    Operation = operation.ToString(),
                    Message = GenerateMessage(entityType, entityId, entityDisplayName, operation),
                    CreatedAt = DateTime.Now,
                    CreatedBy = userName ?? "System",
                    IsRead = false
                };

                var jsonMessage = JsonConvert.SerializeObject(notification);
                var message = new Message(jsonMessage)
                {
                    Label = $"{entityType} {operation}",
                    Priority = MessagePriority.Normal
                };

                _queue.Send(message);
            }
            catch (Exception ex)
            {
                // Log error but don't break the main operation
                System.Diagnostics.Debug.WriteLine($"Failed to send notification: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns up to <paramref name="maxCount"/> pending notifications without
        /// removing them from the queue. Each returned notification carries the
        /// underlying MSMQ message id in <see cref="Notification.MessageId"/>; pass
        /// that value to <see cref="MarkAsRead(string)"/> to remove the message.
        /// </summary>
        public IList<Notification> PeekNotifications(int maxCount)
        {
            var results = new List<Notification>();
            if (maxCount <= 0)
            {
                return results;
            }

            MessageEnumerator enumerator = null;
            try
            {
                enumerator = _queue.GetMessageEnumerator2();
                while (results.Count < maxCount && enumerator.MoveNext())
                {
                    var message = enumerator.Current;
                    try
                    {
                        var jsonContent = message.Body.ToString();
                        var notification = JsonConvert.DeserializeObject<Notification>(jsonContent);
                        if (notification != null)
                        {
                            notification.MessageId = message.Id;
                            results.Add(notification);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to deserialize notification: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to peek notifications: {ex.Message}");
            }
            finally
            {
                enumerator?.Close();
            }

            return results;
        }

        /// <summary>
        /// Acknowledges (removes) the queued notification with the given MSMQ message id.
        /// Returns true if a message was removed.
        /// </summary>
        public bool MarkAsRead(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                return false;
            }

            try
            {
                _queue.ReceiveById(messageId, TimeSpan.FromSeconds(1));
                return true;
            }
            catch (InvalidOperationException)
            {
                // Message no longer in queue (already acknowledged).
                return false;
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.MessageNotFound
                                                || ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to mark notification as read: {ex.Message}");
                return false;
            }
        }

        private string GenerateMessage(string entityType, string entityId, string entityDisplayName, EntityOperation operation)
        {
            var displayText = !string.IsNullOrWhiteSpace(entityDisplayName)
                ? $"{entityType} '{entityDisplayName}'"
                : $"{entityType} (ID: {entityId})";

            switch (operation)
            {
                case EntityOperation.CREATE:
                    return $"New {displayText} has been created";
                case EntityOperation.UPDATE:
                    return $"{displayText} has been updated";
                case EntityOperation.DELETE:
                    return $"{displayText} has been deleted";
                default:
                    return $"{displayText} operation: {operation}";
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _queue?.Dispose();
            }

            _disposed = true;
        }
    }
}
