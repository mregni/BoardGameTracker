import { Link } from "@tanstack/react-router";
import { Gamepad2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import { NavLink } from "./NavLink";

export const MenuBar = () => {
	const { t } = useTranslation();

	return (
		<nav className="fixed top-0 left-0 right-0 z-50 bg-slate-900/80 backdrop-blur-md border-b border-white/10">
			<div className="max-w-7xl mx-auto px-6">
				<div className="flex items-center justify-between h-16">
					<Link to="/" className="flex items-center gap-3 group">
						<div className="bg-purple-600 rounded-lg p-2 group-hover:bg-purple-500 transition-colors">
							<Gamepad2 className="w-6 h-6 text-white" />
						</div>
						<span className="text-xl font-semibold text-white">
							{t("nav.title")}
						</span>
					</Link>

					<div className="flex items-center gap-8">
						<NavLink to="/info" label={t("nav.info")} />
						<NavLink to="/documentation" label={t("nav.documentation")} />
					</div>
				</div>
			</div>
		</nav>
	);
};
