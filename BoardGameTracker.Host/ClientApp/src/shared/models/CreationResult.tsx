export interface CreationResult<T> {
  type: CreationResultType;
  reason: string | null;
  data: T | null;
}

export enum CreationResultType {
  Success = 0,
  Failed = 1
}