export enum RecognitionVisibility {
  Public = 0,
  Department = 1,
  Private = 2,
}

export enum LeaderboardPeriod {
  Month = 0,
  Quarter = 1,
  Year = 2,
  AllTime = 3,
}

export interface RecognitionType {
  id: string;
  code: string;
  name: string;
  description: string | null;
  badgeIcon: string | null;
  badgeColor: string | null;
  defaultPoints: number;
}

export interface Recognition {
  id: string;
  senderEmployeeId: string;
  senderDisplayName: string;
  recipientEmployeeId: string;
  recipientDisplayName: string;
  recognitionTypeId: string;
  recognitionTypeName: string;
  badgeIcon: string | null;
  badgeColor: string | null;
  message: string;
  points: number;
  visibility: RecognitionVisibility;
  recognisedOn: string;
}

export interface LeaderboardEntry {
  rank: number;
  employeeId: string;
  displayName: string;
  jobTitle: string | null;
  profilePictureFileId: string | null;
  points: number;
  recognitionCount: number;
}

export interface Leaderboard {
  period: LeaderboardPeriod;
  periodStart: string;
  generatedAt: string | null;
  entries: LeaderboardEntry[];
  me: LeaderboardEntry | null;
}
