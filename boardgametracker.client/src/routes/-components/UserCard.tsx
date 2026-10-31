import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import LogOut from "@/assets/icons/log-out.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { useAuth } from "@/hooks/useAuth";
import { useLogout } from "@/hooks/useLogout";

export const UserCard = ({ className }: { className?: string }) => {
	const user = useAuth((s) => s.user);
	const isAuthenticated = useAuth((s) => s.isAuthenticated);
	const authStatus = useAuth((s) => s.authStatus);
	const { t } = useTranslation("common");
	const handleLogout = useLogout();

	if (!authStatus?.authEnabled || !isAuthenticated || !user) return null;

	const name = user.displayName || user.username;

	return (
		<div className={cx("px-2 py-2 bg-white/5 flex items-center gap-2", className)}>
			<div className="shrink-0">
				<BgtAvatar image={user.profileImage} title={name} colorKey={user.id} />
			</div>
			<div className="flex-1 min-w-0">
				<div className="text-sm font-medium text-white/80 truncate">{name}</div>
			</div>
			<BgtIconButton
				icon={<LogOut className="size-4" />}
				onClick={handleLogout}
				intent="subtile"
				aria-label={t("common:logout")}
			/>
		</div>
	);
};
