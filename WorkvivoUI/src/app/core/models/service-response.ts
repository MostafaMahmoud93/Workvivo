/** Mirrors Workvivo.Application.Bases.ServiceResponse<T>. */
export interface ServiceResponse<T> {
  success: boolean;
  message: string;
  data: T;
}
