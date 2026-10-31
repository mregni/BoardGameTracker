import { BgtMenuItem } from "@/components/BgtMenu/BgtMenuItem";
import { BgtMenuLogo } from "@/components/BgtMenu/BgtMenuLogo";
import { useMenuInfo } from "../-hooks/useMenuInfo";
import { UserCard } from "./UserCard";
import { VersionCard } from "./VersionCard";

export const Sidebar = () => {
	const { counts, versionInfo, menuItems } = useMenuInfo();

	return (
		<div className="hidden md:block">
			<aside className="w-56 bg-background h-full border-r border-white/10 flex flex-col gap-1">
				<div className="pt-2">
					<BgtMenuLogo />
				</div>

				<UserCard className="mx-3 rounded-lg" />

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
