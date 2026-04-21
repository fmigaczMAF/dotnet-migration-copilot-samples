# Real-Time Notification System

This project now includes a real-time notification system that broadcasts whenever entity operations (create, update, delete) are performed in the system.

## Overview

The notification system uses **Microsoft Message Queuing (MSMQ)** as the underlying technology to provide reliable, real-time notifications to connected clients.

## Features

- **Real-time notifications**: Users receive immediate notifications when entities are modified
- **Entity coverage**: Monitors Students, Courses, Instructors, and Departments
- **Operation tracking**: Tracks CREATE, UPDATE, and DELETE operations
- **Open access**: This sample does not implement authentication; any visitor sees notifications
- **Non-intrusive UI**: Notifications appear in the top-right corner with auto-dismiss
- **Reliable delivery**: Uses MSMQ to buffer messages until a client acknowledges them

## How It Works

### Backend Components

1. **NotificationService**: Handles MSMQ operations for sending and peeking messages,
   plus removing them when acknowledged
2. **BaseController**: Base class that all controllers inherit from to send notifications
3. **Notification Model**: Entity to represent notification data
4. **NotificationsController**: API endpoints for retrieving and acknowledging notifications

### Frontend Components

1. **notifications.css**: Styling for notification UI elements
2. **notifications.js**: JavaScript polling system that checks for new notifications and
   acknowledges them once they have been displayed
3. **Layout integration**: Notification assets are loaded for every page

### Technology Stack

- **Microsoft Message Queuing (MSMQ)**: Message queue technology
- **ASP.NET MVC**: Web framework
- **Entity Framework Core**: Data access for the rest of the application
- **JavaScript**: Frontend polling and UI updates
- **Bootstrap**: UI styling

## Configuration

The notification system is configured in `Web.config`:

```xml
<appSettings>
    <add key="NotificationQueuePath" value=".\Private$\ContosoUniversityNotifications"/>
</appSettings>
```

## Queue Details

- **Queue Path**: `.\Private$\ContosoUniversityNotifications`
- **Queue Type**: Private queue, auto-created if not exists
- **Permissions**: Full control for "Everyone" (suitable for development)
- **Message Format**: JSON serialized notification objects

## Usage

### For Visitors

1. Open the application in a browser
2. Navigate to **Notifications** in the main menu to view the dashboard
3. Perform any CRUD operation on entities (Students, Courses, Instructors, Departments)
4. Watch for notifications appearing in the top-right corner
5. Notifications auto-dismiss after 1 minute or can be manually closed

### For Developers

To add notification support to a new controller:

1. Inherit from `BaseController` instead of `Controller`
2. Remove the private `SchoolContext db` declaration (handled by base class)
3. Call `SendEntityNotification()` after successful save operations:

```csharp
// Example: After creating a student
db.Students.Add(student);
db.SaveChanges();
SendEntityNotification("Student", student.ID.ToString(), EntityOperation.CREATE);
```

### Delivery model

- The HTTP `GET /Notifications/GetNotifications` endpoint **peeks** the queue
  without removing messages, so multiple browser tabs can each see a new
  notification.
- After a client renders a notification it issues `POST /Notifications/MarkAsRead`
  with the message id, which removes the message from the queue so it is not
  redelivered indefinitely.

## Notification Types

- **CREATE**: Green notification for entity creation
- **UPDATE**: Blue notification for entity updates  
- **DELETE**: Orange notification for entity deletion

## System Requirements

- Windows operating system (for MSMQ)
- MSMQ feature enabled (Windows Features → Message Queuing)
- .NET Framework 4.8
- SQL Server (for Entity Framework)

## Testing the System

1. Access the **Notifications** dashboard from the main menu
2. Click on any of the "Create new..." buttons provided
3. Complete a create/edit/delete operation
4. Observe the notification appearing in the top-right corner

## Troubleshooting

### Common Issues

1. **MSMQ not installed**: Enable "Message Queuing" in Windows Features
2. **Queue permissions**: Ensure the application pool identity has access to create private queues
3. **No notifications appearing**: Check browser console for JavaScript errors
4. **Queue not created**: Verify the application has permissions to create private queues

### Development Notes

- Notifications are sent synchronously to MSMQ but failures are swallowed so they
  do not block the originating request
- Failed notification sends are logged to debug output but don't affect user operations
- JavaScript polling occurs every 5 seconds
- Maximum of 5 notifications are displayed simultaneously

## Architecture Benefits

- **Decoupled**: MSMQ ensures notifications don't affect main application performance
- **Reliable**: Messages persist even if the web application restarts
- **Scalable**: Multiple connected clients can each receive the same notification
- **Maintainable**: Clear separation between notification logic and business logic

## Future Enhancements

Potential improvements for production use:

1. **SignalR integration**: Real-time push notifications instead of polling
2. **Email notifications**: Send email alerts for critical operations
3. **Notification persistence**: Store notifications in database for audit trail
4. **User preferences**: Allow users to configure notification types
5. **Batch operations**: Group related notifications to reduce noise
6. **Advanced filtering**: Filter notifications by entity type or operation
