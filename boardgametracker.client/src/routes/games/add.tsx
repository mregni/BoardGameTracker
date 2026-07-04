import { createFileRoute, useNavigate, useRouter } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Database from "@/assets/icons/database.svg?react";
import Keyboard from "@/assets/icons/keyboard.svg?react";
import MagnifyingGlass from "@/assets/icons/magnifying-glass.svg?react";
import BgtBigButton from "@/components/BgtButton/BgtBigButton";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";

export const Route = createFileRoute("/games/add")({
	component: RouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation(["game", "common", "settings"]);
	const navigate = useNavigate();
	const router = useRouter();
	const { settings } = useSettingsData();

	const bggEnabled = settings?.bggStatus?.isConfigured ?? false;

	return (
		<BgtPage>
			<BgtPageContent className="flex-1 items-center">
				<BgtCard title={t("game:new.title")} className="w-full max-w-xl my-auto">
					<p className="text-cancel mb-4">{t("game:new.description")}</p>
					<div className="flex flex-col gap-4 mb-6">
						{!bggEnabled && (
							<BgtStatus
								variant="warning"
								title={t("settings:bgg.status.not-configured")}
								description={t("settings:bgg.status.not-configured-description")}
							/>
						)}

						<BgtBigButton
							title={t("game:new.bgg-title")}
							subText={t("game:new.bgg-subtext")}
							icon={MagnifyingGlass}
							onClick={() => navigate({ to: "/games/bgg" })}
							disabled={!bggEnabled}
						/>
						<BgtBigButton
							title={t("game:new.manual-title")}
							subText={t("game:new.manual-subtext")}
							icon={Keyboard}
							onClick={() => navigate({ to: "/games/new" })}
						/>
						<BgtBigButton
							title={t("game:new.bgg-import-title")}
							subText={t("game:new.bgg-import-subtext")}
							icon={Database}
							onClick={() => navigate({ to: "/games/import/start" })}
							disabled={!bggEnabled}
						/>
					</div>
					<BgtButton variant="cancel" className="w-full" onClick={() => router.history.back()}>
						{t("common:cancel")}
					</BgtButton>
				</BgtCard>
			</BgtPageContent>
		</BgtPage>
	);
}
