export interface BgtSelectItem {
	value: number | string;
	label: string;
	group?: string;
	disabled?: boolean;
	hint?: string;
}

export interface BgtSelectImageItem extends BgtSelectItem {
	image: string | null;
}
