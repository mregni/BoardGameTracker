import { useTranslation } from "react-i18next";

const block = "rounded-md bg-white/10 animate-pulse motion-reduce:animate-none";

export const GameNightListSkeleton = () => {
	const { t } = useTranslation("game-nights");

	return (
		<div className="flex flex-col gap-6" role="status" aria-busy="true" data-testid="game-nights-skeleton">
			<span className="sr-only">{t("loading")}</span>
			<div className="flex flex-col gap-3" aria-hidden="true">
				<div className={`${block} h-5 w-28`} />
				<div className="grid grid-cols-[3.5rem_minmax(0,1fr)] gap-3 rounded-lg border border-primary/20 bg-primary/10 p-4 md:grid-cols-[6rem_minmax(0,1fr)] md:gap-6 md:p-6">
					<div className={`${block} h-20 md:h-24`} />
					<div className="flex flex-col gap-3">
						<div className={`${block} h-4 w-32`} />
						<div className={`${block} h-7 w-2/3`} />
						<div className={`${block} h-4 w-1/2`} />
						<div className={`${block} mt-2 h-2 w-full`} />
					</div>
				</div>
			</div>
			<div className="flex flex-col gap-3" aria-hidden="true">
				<div className={`${block} h-5 w-20`} />
				{[0, 1, 2].map((row) => (
					<div key={row} className="grid grid-cols-[3.25rem_minmax(0,1fr)] items-center gap-3 px-2 py-3">
						<div className={`${block} h-12`} />
						<div className="flex flex-col gap-2">
							<div className={`${block} h-4 w-1/3`} />
							<div className={`${block} h-3 w-1/2`} />
						</div>
					</div>
				))}
			</div>
		</div>
	);
};
