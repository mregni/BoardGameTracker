interface SubHeaderProps {
	content: string;
}

export const SubHeader = ({ content }: Readonly<SubHeaderProps>) => (
	<h3 className="pl-4 border-l-[3px] border-purple-500/50 mt-8 mb-4 text-slate-300">
		{content}
	</h3>
);
