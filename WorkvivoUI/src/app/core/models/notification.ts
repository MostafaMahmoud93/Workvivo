/** Mirrors the server's NotificationType enum; the value is what the API sends. */
export enum NotificationType {
  General = 0,
  PostComment = 1,
  CommentReply = 2,
  Mention = 3,
  Reaction = 4,
  Recognition = 5,
  Announcement = 6,
  EventInvitation = 7,
  EventReminder = 8,
  SurveyInvitation = 9,
  PollInvitation = 10,
  CommunityInvitation = 11,
  CommunityJoinRequest = 12,
  CommunityMembershipApproved = 13,
  ContentModerated = 14,
  NewFollower = 15,
  Birthday = 16,
  WorkAnniversary = 17,
}

export enum DigestFrequency {
  Immediate = 0,
  Hourly = 1,
  Daily = 2,
  Weekly = 3,
  Never = 4,
}

export interface AppNotification {
  /** The recipient's own copy - what "mark read" addresses. */
  id: string;
  notificationId: string;
  type: NotificationType;
  header: string;
  content: string;
  redirectUrl: string | null;
  entityType: number;
  entityId: string | null;
  actorEmployeeId: string | null;
  actorDisplayName: string | null;
  actorProfilePictureFileId: string | null;
  isSeen: boolean;
  createdDate: string;
}

export interface NotificationPreference {
  type: NotificationType;
  inApp: boolean;
  email: boolean;
  emailFrequency: DigestFrequency;
  isExplicit: boolean;
}

/** What the hub pushes. Matches RealtimeNotificationDto. */
export interface RealtimeNotification {
  notification: AppNotification;
  unread: number;
}

/**
 * Glyph per type, so a list of notifications is scannable without reading every line.
 *
 * Partial on purpose: not every type has a glyph worth showing, and typing it as a
 * complete record would tell the compiler a lookup always succeeds when at runtime it
 * returns undefined - which is how a fallback gets removed as "unnecessary".
 */
export const NOTIFICATION_GLYPHS: Partial<Record<NotificationType, string>> = {
  [NotificationType.PostComment]: '\u{1F4AC}',
  [NotificationType.CommentReply]: '\u{21A9}\u{FE0F}',
  [NotificationType.Mention]: '\u{0040}',
  [NotificationType.Reaction]: '\u{2764}\u{FE0F}',
  [NotificationType.Recognition]: '\u{1F3C6}',
  [NotificationType.Announcement]: '\u{1F4E2}',
  [NotificationType.EventInvitation]: '\u{1F4C5}',
  [NotificationType.EventReminder]: '\u{23F0}',
  [NotificationType.SurveyInvitation]: '\u{1F4CB}',
  [NotificationType.PollInvitation]: '\u{1F4CA}',
  [NotificationType.NewFollower]: '\u{1F464}',
  [NotificationType.Birthday]: '\u{1F382}',
  [NotificationType.WorkAnniversary]: '\u{1F389}',
};

/** Shown for a type with no glyph of its own. */
export const DEFAULT_NOTIFICATION_GLYPH = '\u{1F514}';
