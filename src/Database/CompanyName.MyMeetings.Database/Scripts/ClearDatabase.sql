DELETE FROM [administration].[InboxMessages]

DELETE FROM [administration].[InternalCommands]

DELETE FROM [administration].[OutboxMessages]

DELETE FROM [administration].[MeetingGroupProposals]

DELETE FROM [administration].[Members]

DELETE FROM [meetings].[InboxMessages]

DELETE FROM [meetings].[InternalCommands]

DELETE FROM [meetings].[OutboxMessages]

DELETE FROM [meetings].[MeetingAttendees]

DELETE FROM [meetings].[MeetingGroupMembers]

DELETE FROM [meetings].[MeetingGroupProposals]

DELETE FROM [meetings].[MeetingGroups]

DELETE FROM [meetings].[MeetingNotAttendees]

DELETE FROM [meetings].[Meetings]

DELETE FROM [meetings].[MeetingWaitlistMembers]

DELETE FROM [meetings].[MeetingMemberCommentLikes]

DELETE FROM [meetings].[Members]

DELETE FROM [meetings].[MeetingComments]

DELETE FROM [payments].[InboxMessages]

DELETE FROM [payments].[InternalCommands]

DELETE FROM [payments].[OutboxMessages]

IF OBJECT_ID(N'[payments].[pc_events]', N'U') IS NOT NULL DELETE FROM [payments].[pc_events]
IF OBJECT_ID(N'[payments].[pc_streams]', N'U') IS NOT NULL DELETE FROM [payments].[pc_streams]
IF OBJECT_ID(N'[payments].[pc_event_progression]', N'U') IS NOT NULL DELETE FROM [payments].[pc_event_progression]
IF OBJECT_ID(N'[payments].[Messages]', N'U') IS NOT NULL DELETE FROM payments.[Messages]
IF OBJECT_ID(N'[payments].[Streams]', N'U') IS NOT NULL DELETE FROM payments.Streams

DELETE FROM payments.SubscriptionDetails

DELETE FROM [payments].[SubscriptionCheckpoints]

DELETE FROM [payments].PriceListItems

DELETE FROM [payments].SubscriptionPayments

DELETE FROM [payments].MeetingFees

DELETE FROM [payments].[Payers]

DELETE FROM [users].[InboxMessages]

DELETE FROM [users].[InternalCommands]

DELETE FROM [users].[OutboxMessages]

DELETE FROM [users].[Users]

DELETE FROM [users].[RolesToPermissions]

DELETE FROM [users].[UserRoles]

DELETE FROM [users].[Permissions]

DELETE FROM [registrations].[UserRegistrations]