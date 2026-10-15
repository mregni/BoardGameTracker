import { createFileRoute, Link, useNavigate, useRouter } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import Database from "@/assets/icons/database.svg?react";
import Keyboard from "@/assets/icons/keyboard.svg?react";
import MagnifyingGlass from "@/assets/icons/magnifying-glass.svg?react";
import BgtBigButton from "@/components/BgtButton/BgtBigButton";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";
import BgtPageHeader from "@/components/BgtLayout/BgtPageHeader";
import { BgtStatus } from "@/components/BgtStatus/BgtStatus";
import { RequireWrite } from "@/routes/-components/RequireWrite";
import { useGoBack } from "@/routes/-hooks/useGoBack";
import { useSettingsData } from "@/routes/settings/-hooks/useSettingsData";

export const Route = createFileRoute("/games/add")({
	component: WriteRouteComponent,
});

function RouteComponent() {
	const { t } = useTranslation(["game", "common", "settings"]);
	const navigate = useNavigate();
	const router = useRouter();
	const goBack = useGoBack("/games");
	const { settings } = useSettingsData();

	const bggEnabled = settings?.bggStatus?.isConfigured ?? false;

	return (
		<BgtPage>
			<BgtPageHeader backAction={goBack} backText={t("common:back")} header={t("game:new.title")} />
			<BgtPageContent className="items-center">
				<BgtCard className="w-full max-w-xl">
					<p className="text-cancel mb-4">{t("game:new.description")}</p>
					<div className="flex flex-col gap-4 mb-6">
						{settings && !bggEnabled && (
							<BgtStatus
								variant="warning"
								title={t("settings:bgg.status.not-configured")}
								description={
									<>
										{t("game:new.bgg-missing.description")}{" "}
										<Link
											to="/settings"
											search={{ tab: "bgg" }}
											className="font-medium text-white underline hover:text-orange-300"
										>
											{t("game:new.bgg-missing.link")}
										</Link>
									</>
								}
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

function WriteRouteComponent() {
	return (
		<RequireWrite>
			<RouteComponent />
		</RequireWrite>
	);
}
