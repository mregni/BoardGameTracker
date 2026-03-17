import type { LucideIcon } from "lucide-react";

interface SectionHeaderProps {
	icon: LucideIcon;
	title: string;
}

export const SectionHeader = ({ icon: Icon, title }: SectionHeaderProps) => (
	<div className="flex items-center gap-3 mb-6">
		<div className="bg-purple-600 rounded-lg p-2">
			<Icon className="w-5 h-5 text-white" />
		</div>
		<h2 className="text-3xl text-white">{title}</h2>
	</div>
);
