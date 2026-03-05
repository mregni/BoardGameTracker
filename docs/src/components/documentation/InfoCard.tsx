interface InfoCardProps {
	title: string;
	children: React.ReactNode;
}

export const InfoCard = ({ title, children }: InfoCardProps) => (
	<div className="bg-slate-800 rounded-lg p-6 border border-white/10 text-white">
		<h3 className="text-white mb-3">{title}</h3>
		{children}
	</div>
);
