import type { LucideIcon } from "lucide-react";

interface FeatureCardProps {
	icon: LucideIcon;
	title: string;
	description: string;
}

export const FeatureCard = ({ icon: Icon, title, description }: FeatureCardProps) => {
	return (
		<div className="bg-slate-900/50 border border-white/10 rounded-xl p-6 hover:border-purple-500/50 transition-colors">
			<div className="bg-purple-600 rounded-lg p-3 w-fit mb-4">
				<Icon className="w-6 h-6 text-white" />
			</div>
			<h3 className="text-xl text-white mb-3">{title}</h3>
			<p className="text-slate-300">{description}</p>
		</div>
	);
};
