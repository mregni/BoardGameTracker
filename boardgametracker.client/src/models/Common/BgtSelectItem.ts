export interface BgtSelectItem {
  value: number;
  label: string;
}

export interface BgtSelectImageItem extends BgtSelectItem {
  image: string | null;
}
