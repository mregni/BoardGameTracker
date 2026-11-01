import { useTranslation } from "react-i18next";
import { BgtPage } from "@/components/BgtLayout/BgtPage";
import { BgtPageContent } from "@/components/BgtLayout/BgtPageContent";

const block = "skeleton motion-safe:animate-shimmer";

export const DashboardSkeleton = () => {
	const { t } = useTranslation("common");

	return (
		<BgtPage>
			<BgtPageContent>
				<div role="status" aria-label={t("loading-data")} className="flex flex-col gap-3 xl:gap-6">
					<div className={`${block} h-16`} />
					<div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
						{["games", "players", "sessions", "time"].map((key) => (
							<div key={key} className={`${block} h-28`} />
						))}
					</div>
					<div className="grid grid-cols-1 items-start gap-4 xl:grid-cols-[minmax(0,1.35fr)_minmax(0,1fr)]">
						<div className={`${block} h-96`} />
						<div className="flex flex-col gap-4">
							<div className={`${block} h-56`} />
							<div className={`${block} h-36`} />
						</div>
					</div>
				</div>
			</BgtPageContent>
		</BgtPage>
	);
};
