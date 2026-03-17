import { BulletItem } from "../BulletItem";

interface BulletListProps {
	items: string[];
}

export const BulletList = ({ items }: BulletListProps) => (
	<ul className="space-y-2 text-white">
		{items.map((item) => (
			<BulletItem key={item} text={item} />
		))}
	</ul>
);
