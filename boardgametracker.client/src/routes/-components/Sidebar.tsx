import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import LogOut from "@/assets/icons/log-out.svg?react";
import User from "@/assets/icons/user.svg?react";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtMenuItem } from "@/components/BgtMenu/BgtMenuItem";
import { BgtMenuLogo } from "@/components/BgtMenu/BgtMenuLogo";
import { useAuth } from "@/hooks/useAuth";
import { useMenuInfo } from "../-hooks/useMenuInfo";
import { VersionCard } from "./VersionCard";

export const Sidebar = () => {
	const { t } = useTranslation("auth");
	const navigate = useNavigate();
	const { counts, versionInfo, menuItems } = useMenuInfo();
	const { user, isAuthenticated, authStatus, logout } = useAuth();

	const showAuth = authStatus?.authEnabled && !authStatus.bypassEnabled;

	const handleLogout = async () => {
		await logout();
		navigate({ to: "/login" });
	};

	return (
		<div className="hidden md:block">
			<aside className="w-64 bg-background h-full border-r border-white/10 flex flex-col gap-2">
				<div className="pt-4">
					<BgtMenuLogo />
				</div>

				{showAuth && isAuthenticated && user && (
					<div className="mx-3 px-2 rounded-lg bg-white/5 flex items-center gap-2 py-2">
						<div className="w-6 h-6 bg-white/5 rounded-full flex items-center justify-center shrink-0">
							<User className="text-white/40" />
						</div>
						<div className="flex-1 min-w-0">
							<div className="text-xs text-white/60 truncate">{user.username}</div>
						</div>
						<BgtIconButton icon={<LogOut className="size-4" />} onClick={handleLogout} intent="subtile" />
					</div>
				)}

				<nav className="flex-1 px-3">
					{menuItems.map((x) => (
						<BgtMenuItem key={x.path} item={x} count={counts?.find((y) => x.path.endsWith(y.key))?.value} />
					))}
				</nav>

				<VersionCard versionInfo={versionInfo} />
			</aside>
		</div>
	);
};
