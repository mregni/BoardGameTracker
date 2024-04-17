export interface PlayerScoring {
  dateTime: Date;
  series: XValue[];
}

export interface XValue {
  id: string;
  value: number;
}