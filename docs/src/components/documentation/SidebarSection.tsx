import { ChevronDown, ChevronRight } from "lucide-react";
import { useTranslation } from "react-i18next";
import { SidebarItem } from "./SidebarItem";
import type { ActivePage, MenuSection } from "./types";

interface SidebarSectionProps {
	sectionKey: ActivePage;
	section: MenuSection;
	isExpanded: boolean;
	activeHash: string;
	onToggle: (section: string) => void;
}

export const SidebarSection = ({
	sectionKey,
	section,
	isExpanded,
	activeHash,
	onToggle,
}: Readonly<SidebarSectionProps>) => {
	const { t } = useTranslation();

	return (
		<div className="mb-4">
			<button
				type="button"
				onClick={() => onToggle(sectionKey)}
				className="flex items-center gap-2 w-full text-left text-white font-semibold mb-2 hover:text-purple-400 transition-colors"
			>
				{isExpanded ? (
					<ChevronDown className="w-4 h-4" />
				) : (
					<ChevronRight className="w-4 h-4" />
				)}
				{t(section.titleKey)}
			</button>

			{isExpanded && (
				<ul className="space-y-1 ml-6">
					{section.items.map((item, index) => (
						<li key={item.id}>
							<SidebarItem
								item={item}
								to={section.path}
								isActive={activeHash === item.id}
								isFirst={index === 0}
							/>
						</li>
					))}
				</ul>
			)}
		</div>
	);
};
