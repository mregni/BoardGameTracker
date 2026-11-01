import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import MapPin from "@/assets/icons/map-pin.svg?react";
import PencilIcon from "@/assets/icons/pencil.svg?react";
import Play from "@/assets/icons/play.svg?react";
import TrashIcon from "@/assets/icons/trash.svg?react";
import User from "@/assets/icons/user.svg?react";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtHeading } from "@/components/BgtHeading/BgtHeading";
import { BgtIconButton } from "@/components/BgtIconButton/BgtIconButton";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightDateBox } from "@/components/GameNight/GameNightDateBox";
import type { GameNight, Settings } from "@/models";
import { toDisplay, toDisplayTime } from "@/utils/dateUtils";
import { relativeDay } from "@/utils/relativeDay";
import { isInsidePlayWindow, isNightInProgress } from "../-utils/gameNightPlay";
import { NightEndAction } from "./NightEndAction";

interface Props {
	gameNight: GameNight;
	settings: Settings;
	isOver: boolean;
	isPlanned: boolean;
	canWrite: boolean;
	hasRunningGames: boolean;
	onEdit: () => void;
	onDelete: () => void;
	onEnd: () => void;
	onReopen: () => void;
	onStart: () => void;
	onLog: () => void;
}

const phoneButton = "max-md:py-3 max-md:text-sm";

export const GameNightDetailHeader = (props: Props) => {
	const {
		gameNight,
		settings,
		isOver,
		isPlanned,
		canWrite,
		hasRunningGames,
		onEdit,
		onDelete,
		onEnd,
		onReopen,
		onStart,
		onLog,
	} = props;
	const { t } = useTranslation(["game-nights", "common"]);

	const hasEnded = gameNight.endedAt !== null;
	const canReopen = hasEnded && isInsidePlayWindow(gameNight);
	const showEndAction = canWrite && (canReopen || (!hasEnded && !isOver && !isPlanned));
	const showLog = canWrite && !hasEnded && !isPlanned;
	const showStart = showLog && !isOver;
	const hasActions = showEndAction || showLog;
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
							"rounded-2xl px-2.5 py-0.5 text-xs font-semibold tabular-nums first-letter:uppercase",
							isOver ? "bg-white/10 text-gray-300" : "bg-primary/20 text-indigo-300",
						)}
					>
						{isOver
							? toDisplay(gameNight.startDate, settings.dateFormat, settings.uiLanguage)
							: isNightInProgress(gameNight)
								? t("card.live")
								: t(isPlanned ? "plan.starts" : "card.when", {
										when: relativeDay(gameNight.startDate, settings.uiLanguage),
										date: toDisplay(gameNight.startDate, settings.dateFormat, settings.uiLanguage),
										time: toDisplayTime(gameNight.startDate, settings.timeFormat, settings.uiLanguage),
									})}
					</span>
					<BgtHeading as="h1" size="5" className="wrap-anywhere">
						{gameNight.title}
					</BgtHeading>
				</div>
				{canWrite ? (
					<div className="flex gap-2 self-start">
						<BgtIconButton size="2" onClick={onEdit} icon={<PencilIcon />} aria-label={t("common:edit")} />
						<BgtIconButton
							size="2"
							intent="subtile-danger"
							onClick={onDelete}
							icon={<TrashIcon />}
							aria-label={t("common:delete.button")}
						/>
					</div>
				) : (
					<span />
				)}
				<div
					className={cx(
						"flex flex-wrap items-center gap-x-4 gap-y-1 self-center text-sm text-gray-400 md:col-start-2",
						hasActions ? "col-span-2 md:col-span-1" : "col-span-3 md:col-span-2",
					)}
				>
					<span className="inline-flex items-center gap-1.5">
						<MapPin className="size-4 shrink-0" />
						{gameNight.location.name}
					</span>
					<span className="inline-flex items-center gap-1.5">
						<User className="size-4 shrink-0" />
						{t("card.host", { host: gameNight.host.name })}
					</span>
				</div>
				{hasActions && (
					<div className="flex items-center justify-end gap-2 self-center" data-testid="night-actions">
						{endAction}
						{showLog && (
							<div
								className={cx(
									showStart
										? "fixed inset-x-0 bottom-(--bottom-nav-h) z-20 grid grid-cols-2 gap-2 border-t border-white/10 bg-background/95 px-4 py-3 backdrop-blur-sm md:static md:flex md:border-0 md:bg-transparent md:p-0 md:backdrop-blur-none"
										: "flex",
								)}
							>
								<BgtButton variant="cancel" size="1" className={phoneButton} onClick={onLog}>
									{t("play.log.button")}
								</BgtButton>
								{showStart && (
									<BgtButton
										variant="primary"
										size="1"
										className={cx(phoneButton, "order-first md:order-none")}
										onClick={onStart}
									>
										<Play className="size-3.5" aria-hidden="true" />
										{t("play.start.button")}
									</BgtButton>
								)}
							</div>
						)}
					</div>
				)}
				{gameNight.notes && (
					<div className="col-span-3 rounded-lg border border-white/10 bg-white/5 p-3 md:col-span-2 md:col-start-2">
						<BgtText size="2" color="gray" className="whitespace-pre-line">
							{gameNight.notes}
						</BgtText>
					</div>
				)}
			</article>
		</BgtCard>
	);
};
