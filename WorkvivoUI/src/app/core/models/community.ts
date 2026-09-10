export enum CommunityPrivacy {
  Public = 0,
  Private = 1,
  Restricted = 2,
}

export enum MembershipStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Banned = 3,
  Left = 4,
}

export enum CommunityMemberRole {
  Member = 0,
  Moderator = 1,
  Owner = 2,
}

export type MembershipDecision = 'Approve' | 'Reject' | 'Ban' | 'Remove';

export interface CommunitySummary {
  id: string;
  slug: string;
  name: string;
  description: string | null;
  privacy: CommunityPrivacy;
  logoFileId: string | null;
  membersCount: number;
  postsCount: number;
  isFeatured: boolean;
  myMembershipStatus: MembershipStatus | null;
  myRole: CommunityMemberRole | null;
  canJoin: boolean;
}

export interface CommunityDetail extends CommunitySummary {
  coverFileId: string | null;
  ownerEmployeeId: string;
  ownerDisplayName: string | null;
  isActive: boolean;
  canRead: boolean;
  canPost: boolean;
  canModerate: boolean;
  canManage: boolean;
  pendingRequestsCount: number;
}

export interface CommunityMember {
  employeeId: string;
  displayName: string;
  jobTitle: string | null;
  profilePictureFileId: string | null;
  role: CommunityMemberRole;
  status: MembershipStatus;
  joinedAt: string | null;
  requestedAt: string;
}

export interface CommunityInvitation {
  id: string;
  communityId: string;
  communityName: string;
  message: string | null;
  invitedByDisplayName: string;
  expiresAt: string;
}
