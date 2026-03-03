import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { BgtMenuItem } from "@/components/BgtMenu/BgtMenuItem";
import { BgtMenuLogo } from "@/components/BgtMenu/BgtMenuLogo";
import { useAuth } from "@/hooks/useAuth";
import { useMenuInfo } from "../-hooks/useMenuInfo";
import { VersionCard } from "./VersionCard";

export const Sidebar = () => {
	const { t } = useTranslation();
	const navigate = useNavigate();
	const { counts, versionInfo, menuItems } = useMenuInfo();
	const { user, isAuthenticated, authStatus, logout } = useAuth();

	if (counts === undefined) return null;

	const showAuth = authStatus?.authEnabled && !authStatus.bypassEnabled;

	const handleLogout = async () => {
		await logout();
		navigate({ to: "/login" });
	};

	return (
		<div className="hidden md:block">
			<aside className="w-64 bg-background h-full border-r border-white/10 flex flex-col">
				<div className="p-6">
					<BgtMenuLogo />
				</div>

				<nav className="flex-1 px-3">
					{menuItems.map((x) => (
						<BgtMenuItem key={x.path} item={x} count={counts.find((y) => x.path.endsWith(y.key))?.value} />
					))}
				</nav>

				{showAuth && isAuthenticated && user && (
					<div className="px-3 py-2 border-t border-white/10">
						<div className="flex items-center justify-between px-3 py-2">
							<div className="min-w-0">
								<p className="text-sm font-medium text-white truncate">{user.displayName ?? user.username}</p>
								<p className="text-xs text-gray-400 truncate">{user.roles.join(", ")}</p>
							</div>
							<button
								type="button"
								onClick={handleLogout}
								className="text-xs text-gray-400 hover:text-white transition-colors shrink-0 ml-2"
								title={t("auth.logout")}
							>
								{t("auth.logout")}
							</button>
						</div>
					</div>
				)}

				<VersionCard versionInfo={versionInfo} />
			</aside>
		</div>
	);
};
