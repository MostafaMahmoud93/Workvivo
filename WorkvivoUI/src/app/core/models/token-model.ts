/** Mirrors Workvivo.Domain.Models.TokenModel. */
export interface TokenModel {
  token: string;
  expiration: string;
  userId: string | null;
  isAdmin: boolean | null;
  userType: string | null;
  userActions: string[];
}
