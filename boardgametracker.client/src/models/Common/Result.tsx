export interface Result<T> {
  model: T;
  state: ResultState;
}

export enum ResultState {
  Success = 0,
  Failed = 1,
  Duplicate = 2,
  Found = 3,
  Updated = 4,
}
