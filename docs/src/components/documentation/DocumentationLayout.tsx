import { useLocation } from "@tanstack/react-router";
import { useEffect, useState } from "react";
import { menuItems } from "./menuItems";
import { Sidebar } from "./Sidebar";
import type { ActivePage } from "./types";

interface DocumentationLayoutProps {
	children: React.ReactNode;
	activePage: ActivePage;
}

export const DocumentationLayout = ({
	children,
	activePage,
}: DocumentationLayoutProps) => {
	const location = useLocation();
	const firstItemId = menuItems[activePage].items[0].id;
	const activeHash = location.hash || firstItemId;

	useEffect(() => {
		if (!location.hash || location.hash === firstItemId) {
			window.scrollTo({ top: 0 });
		}
	}, [location.hash, firstItemId]);

	const [expandedSections, setExpandedSections] = useState<
		Record<ActivePage, boolean>
	>({
		"getting-started": activePage === "getting-started",
		"user-guide": activePage === "user-guide",
		extra: activePage === "extra",
	});

	const toggleSection = (section: string) => {
		setExpandedSections((prev) => ({
			...prev,
			[section]: !prev[section as ActivePage],
		}));
	};

	return (
		<>
			<Sidebar
				activePage={activePage}
				activeHash={activeHash}
				expandedSections={expandedSections}
				onToggleSection={toggleSection}
			/>
			<main className="ml-64 px-6 py-12 bg-slate-950/50 min-h-screen">
				<div className="max-w-4xl mx-auto">{children}</div>
			</main>
		</>
	);
};
