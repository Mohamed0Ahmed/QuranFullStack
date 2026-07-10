export interface ApiResponse<T> {
  isSuccess: boolean;
  message: string | null;
  data?: T | null;
  errors?: string[] | null;
}
