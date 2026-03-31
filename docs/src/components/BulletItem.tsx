interface BulletItemProps {
	text: string;
}

export const BulletItem = ({ text }: Readonly<BulletItemProps>) => {
	return (
		<li className="flex items-center gap-3">
			<span className="text-purple-400 text-[8px]">●</span>
			<span>{text}</span>
		</li>
	);
};
