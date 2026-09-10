// Re-exported so the existing imports keep working; the definition now lives in
// paging.ts, which is where a type used by four features belongs.
export type { PagedResult } from './paging';

export interface EmployeeListItem {
  id: string;
  displayName: string;
  fullNameAr: string | null;
  employeeNumber: string;
  jobTitle: string | null;
  department: string | null;
  location: string | null;
  email: string | null;
  mobile: string | null;
  profilePictureFileId: string | null;
  isFollowedByMe: boolean;
}

export interface PersonSummary {
  id: string;
  displayName: string;
  jobTitle: string | null;
  profilePictureFileId: string | null;
}

export interface EmployeeSkill {
  id: string;
  name: string;
  endorsementCount: number;
}

export interface EmployeeProfile {
  id: string;
  displayName: string;
  fullNameAr: string | null;
  firstName: string;
  middleName: string | null;
  lastName: string;
  employeeNumber: string;
  email: string | null;
  mobile: string | null;
  extension: string | null;
  biography: string | null;
  departmentId: string | null;
  departmentName: string | null;
  teamId: string | null;
  teamName: string | null;
  locationId: string | null;
  locationName: string | null;
  jobTitleId: string | null;
  jobTitleName: string | null;
  profilePictureFileId: string | null;
  coverPictureFileId: string | null;
  joiningDate: string | null;

  /** Day and month only - the API never sends the birth year. */
  birthDay: number | null;
  birthMonth: number | null;

  preferredLanguage: string;
  followersCount: number;
  followingCount: number;
  recognitionPoints: number;
  isActive: boolean;
  isFollowedByMe: boolean;
  isMe: boolean;
  manager: PersonSummary | null;
  directReports: PersonSummary[];
  skills: EmployeeSkill[];
  interests: string[];
}

export interface LookupItem {
  id: string;
  name: string;
  secondary: string | null;
}

export interface OrganizationLookups {
  departments: LookupItem[];
  teams: LookupItem[];
  locations: LookupItem[];
  jobTitles: LookupItem[];
}

export interface DirectoryFilters {
  search?: string;
  departmentId?: string;
  locationId?: string;
  jobTitleId?: string;
  pageNumber?: number;
  pageSize?: number;
}
