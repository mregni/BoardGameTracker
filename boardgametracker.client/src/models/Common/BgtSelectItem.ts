export interface BgtSelectItem {
	value: number | string;
	label: string;
	group?: string;
}

export interface BgtSelectImageItem extends BgtSelectItem {
	image: string | null;
}
