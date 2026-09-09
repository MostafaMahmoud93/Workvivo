/** Mirrors Workvivo.Domain.Models.Auth.AuthenticatedUser. */
export interface AuthenticatedUser {
  userId: string;
  userName: string;
  displayName: string | null;
  email: string | null;
  employeeId: string | null;
  isAdmin: boolean;
  userType: string | null;
  preferredLanguage: string | null;
  profilePictureFileId: string | null;

  /**
   * What the interface may offer. Presentation only - every one of these is checked
   * again server-side, so editing this list buys a nicer menu and a run of 403s.
   */
  permissions: string[];
  roles: string[];
}

/**
 * Mirrors Workvivo.API.Controllers.Auth.SessionResponse.
 *
 * Note what is absent: the refresh token. It never reaches JavaScript - the API puts
 * it in an HttpOnly cookie, which is the whole point of the arrangement.
 */
export interface SessionResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}
