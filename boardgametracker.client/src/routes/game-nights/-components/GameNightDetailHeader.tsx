import { cx } from "class-variance-authority";
import { format } from "date-fns";
import { useTranslation } from "react-i18next";
import MapPin from "@/assets/icons/map-pin.svg?react";
import User from "@/assets/icons/user.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import type { GameNight, Settings } from "@/models";
import { toDisplay } from "@/utils/dateUtils";
import { relativeDay } from "@/utils/relativeDay";
import { NightEndAction } from "./NightEndAction";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	isOver: boolean;
	canWrite: boolean;
	hasRunningGames: boolean;
	onEdit: () => void;
	onDelete: () => void;
	onEnd: () => void;
	onReopen: () => void;
	onStart: () => void;
	onLog: () => void;
}

export const GameNightDetailHeader = (props: Props) => {
	const { gameNight, settings, isOver, canWrite, hasRunningGames, onEdit, onDelete, onEnd, onReopen, onStart, onLog } =
		props;
	const { t } = useTranslation("game-nights");

	const hasEnded = gameNight.endedAt !== null;
	const showEndAction = canWrite && (hasEnded || !isOver);
	const showLog = canWrite && !hasEnded;
	const showStart = showLog && !isOver;
	const endAction = showEndAction && (
		<NightEndAction hasEnded={hasEnded} hasRunningGames={hasRunningGames} onEnd={onEnd} onReopen={onReopen} />
	);

	return (
		<BgtCard>
			<article className="grid grid-cols-[3.5rem_minmax(0,1fr)_auto] gap-x-3 gap-y-3 md:grid-cols-[4.75rem_minmax(0,1fr)_auto] md:gap-x-4 md:gap-y-2">
				<GameNightDateBox
					date={gameNight.startDate}
					uiLanguage={settings.uiLanguage}
					variant={isOver ? "soft" : "highlight"}
					className="self-start md:row-span-2"
				/>
				<div className="flex min-w-0 flex-col items-start gap-1.5 self-center">
					<span
						className={cx(
							"rounded-full px-2.5 py-0.5 text-xs font-semibold whitespace-nowrap tabular-nums first-letter:uppercase",
							isOver ? "bg-white/10 text-gray-300" : "bg-primary/20 text-indigo-300",
						)}
					>
						{isOver
							? toDisplay(gameNight.startDate, settings.dateFormat, settings.uiLanguage)
							: t("card.when", {
									when: relativeDay(gameNight.startDate, settings.uiLanguage),
									time: format(gameNight.startDate, settings.timeFormat),
								})}
					</span>
					<BgtHeading size="5" className="wrap-anywhere">
						{gameNight.title}
					</BgtHeading>
				</div>
				{canWrite ? (
					<div className="flex flex-col-reverse items-end gap-2 self-start">
						{endAction && <div className="md:hidden">{endAction}</div>}
						<BgtEditDeleteButtons onEdit={onEdit} onDelete={onDelete} />
					</div>
				) : (
					<span />
				)}
				<div className="col-span-3 flex flex-wrap items-center justify-between gap-x-4 gap-y-2 md:col-span-2 md:col-start-2">
					<div className="flex flex-wrap gap-x-4 gap-y-1 text-sm text-gray-400">
						<span className="inline-flex items-center gap-1.5">
							<MapPin className="size-4 shrink-0" />
							{gameNight.location.name}
						</span>
						<span className="inline-flex items-center gap-1.5">
							<User className="size-4 shrink-0" />
							{t("card.host", { host: gameNight.host.name })}
						</span>
					</div>
					{(endAction || showLog) && (
						<div className="hidden flex-wrap items-center justify-end gap-2 md:flex">
							{endAction}
							{showLog && (
								<BgtButton variant="cancel" size="1" onClick={onLog}>
									{t("play.log.button")}
								</BgtButton>
							)}
							{showStart && (
								<BgtButton variant="primary" size="1" onClick={onStart}>
									{t("play.start.button")}
								</BgtButton>
							)}
						</div>
					)}
				</div>
				{gameNight.notes && (
					<div className="col-span-3 rounded-lg border border-white/10 bg-white/5 p-3 md:col-span-2 md:col-start-2">
						<BgtText size="2" color="gray">
							{gameNight.notes}
						</BgtText>
					</div>
				)}
			</article>
		</BgtCard>
	);
};
