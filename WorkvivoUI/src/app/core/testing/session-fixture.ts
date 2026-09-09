import { SessionResponse } from '../models/session';

/** A signed-in session as the API returns it. */
export function sessionResponse(permissions: string[] = []): SessionResponse {
  return {
    accessToken: 'issued-token',
    expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    user: {
      userId: '11111111-1111-1111-1111-111111111111',
      userName: 'dev',
      displayName: 'Dev User',
      email: 'dev@example.com',
      employeeId: null,
      isAdmin: true,
      userType: 'MANAG',
      preferredLanguage: 'en',
      profilePictureFileId: null,
      permissions,
      roles: ['Super Admin'],
    },
  };
}

export function successBody(permissions: string[] = []) {
  return { success: true, message: '', data: sessionResponse(permissions) };
}
