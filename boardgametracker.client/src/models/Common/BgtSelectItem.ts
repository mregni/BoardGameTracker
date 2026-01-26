export interface BgtSelectItem {
  value: number | string;
  label: string;
}

export interface BgtSelectImageItem extends BgtSelectItem {
  image: string | null;
}
