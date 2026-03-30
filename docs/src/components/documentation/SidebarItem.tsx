import { Link, useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import type { MenuItem } from "./types";

const itemBaseClasses =
	"block w-full text-left px-3 py-2 rounded text-sm transition-colors";
const activeClasses = "bg-purple-600 text-white";
const inactiveClasses = "text-slate-300 hover:bg-slate-800 hover:text-white";

interface SidebarItemProps {
	item: MenuItem;
	to: string;
	isActive: boolean;
	isFirst?: boolean;
}

export const SidebarItem = ({ item, to, isActive, isFirst }: Readonly<SidebarItemProps>) => {
	const { t } = useTranslation();
	const navigate = useNavigate();

	if (isFirst) {
		return (
			<button
				type="button"
				onClick={() => {
					navigate({ to, hash: item.id });
					window.scrollTo({ top: 0, behavior: "smooth" });
				}}
				className={`${itemBaseClasses} ${isActive ? activeClasses : inactiveClasses}`}
			>
				{t(item.labelKey)}
			</button>
		);
	}

	return (
		<Link
			to={to}
			hash={item.id}
			className={`${itemBaseClasses} ${isActive ? activeClasses : inactiveClasses}`}
		>
			{t(item.labelKey)}
		</Link>
	);
};
