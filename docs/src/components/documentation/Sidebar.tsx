import { menuItems } from "./menuItems";
import { SidebarSection } from "./SidebarSection";
import type { ActivePage } from "./types";

interface SidebarProps {
	activePage: ActivePage;
	activeHash: string;
	expandedSections: Record<ActivePage, boolean>;
	onToggleSection: (section: string) => void;
}

export const Sidebar = ({
	activePage,
	activeHash,
	expandedSections,
	onToggleSection,
}: Readonly<SidebarProps>) => (
	<aside className="fixed top-16 left-0 w-64 h-[calc(100vh-4rem)] bg-slate-900 border-r border-white/10 overflow-y-auto z-40">
		<nav className="p-4">
			{(Object.entries(menuItems) as [ActivePage, (typeof menuItems)[ActivePage]][]).map(
				([sectionKey, section]) => (
					<SidebarSection
						key={sectionKey}
						sectionKey={sectionKey}
						section={section}
						isExpanded={expandedSections[sectionKey]}
						activeHash={sectionKey === activePage ? activeHash : ""}
						onToggle={onToggleSection}
					/>
				),
			)}
		</nav>
	</aside>
);
