export type ReactionType = 'Like' | 'Love' | 'Celebrate' | 'Support' | 'Insightful';

/** Order matches the server enum; the index is what the API expects. */
export const REACTION_TYPES: ReactionType[] = [
  'Like',
  'Love',
  'Celebrate',
  'Support',
  'Insightful',
];

export const REACTION_GLYPHS: Record<ReactionType, string> = {
  Like: '\u{1F44D}',
  Love: '\u{2764}\u{FE0F}',
  Celebrate: '\u{1F389}',
  Support: '\u{1F91D}',
  Insightful: '\u{1F4A1}',
};

export interface Author {
  id: string;
  displayName: string;
  jobTitle: string | null;
  profilePictureFileId: string | null;
}

export interface Attachment {
  id: string;
  type: string;
  fileId: string | null;
  linkUrl: string | null;
  linkTitle: string | null;
  caption: string | null;
}

export interface FeedItem {
  id: string;
  postType: string;
  title: string | null;
  contentHtml: string | null;
  isPinned: boolean;
  isFeatured: boolean;
  isOfficial: boolean;
  commentsEnabled: boolean;
  publishedDate: string;
  author: Author;
  communityId: string | null;
  communityName: string | null;
  commentsCount: number;
  reactionsCount: number;
  reactions: Partial<Record<ReactionType, number>>;
  myReaction: ReactionType | null;
  attachments: Attachment[];
  mentions: { employeeId: string; displayName: string }[];
  canEdit: boolean;
  canDelete: boolean;
}

export interface Comment {
  id: string;
  postId: string;
  parentCommentId: string | null;
  depth: number;
  contentHtml: string | null;
  createdDate: string;
  isEdited: boolean;
  repliesCount: number;
  reactionsCount: number;
  author: Author;
  canReply: boolean;
  canEdit: boolean;
  canDelete: boolean;
}

/** Mirrors CursorPagedResult<T>. No total count - see the DTO for why. */
export interface CursorPage<T> {
  items: T[];
  nextCursor: string | null;
  hasMore: boolean;
}
