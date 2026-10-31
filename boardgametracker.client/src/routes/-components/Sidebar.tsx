import { useTranslation } from "react-i18next";
import LogOut from "@/assets/icons/log-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtMenuItem } from "@/components/BgtMenu/BgtMenuItem";
import { BgtMenuLogo } from "@/components/BgtMenu/BgtMenuLogo";
import { useAuth } from "@/hooks/useAuth";
import { useLogout } from "@/hooks/useLogout";
import { useMenuInfo } from "../-hooks/useMenuInfo";
import { VersionCard } from "./VersionCard";

export const Sidebar = () => {
	const { counts, versionInfo, menuItems } = useMenuInfo();
	const user = useAuth((s) => s.user);
	const isAuthenticated = useAuth((s) => s.isAuthenticated);
	const authStatus = useAuth((s) => s.authStatus);
	const { t } = useTranslation("common");
	const handleLogout = useLogout();

	const showAuth = authStatus?.authEnabled;

	return (
		<div className="hidden md:block">
			<aside className="w-56 bg-background h-full border-r border-white/10 flex flex-col gap-1">
				<div className="pt-2">
					<BgtMenuLogo />
				</div>

				{showAuth && isAuthenticated && user && (
					<div className="mx-3 px-2 rounded-lg bg-white/5 flex items-center gap-2 py-2">
						<div className="shrink-0">
							<BgtAvatar image={user.profileImage} title={user.displayName || user.username} colorKey={user.id} />
						</div>
						<div className="flex-1 min-w-0">
							<div className="text-sm font-medium text-white/80 truncate">{user.displayName || user.username}</div>
						</div>
						<BgtIconButton
							icon={<LogOut className="size-4" />}
							onClick={handleLogout}
							intent="subtile"
							aria-label={t("common:logout")}
						/>
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
