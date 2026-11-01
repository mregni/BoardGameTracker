import { Link } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useTranslation } from "react-i18next";
import LeftRightArrow from "@/assets/icons/left-right-arrow.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import type { PlayerRival } from "@/models";
import { usePlayerById } from "@/routes/-hooks/usePlayerById";

const RIVAL_COUNT = 5;

interface Props {
	playerId: number;
	rivals: PlayerRival[];
}

export const PlayerRivalsCard = (props: Props) => {
	const { playerId, rivals } = props;
	const { t } = useTranslation(["player", "common"]);
	const { playerById } = usePlayerById();

	if (rivals.length === 0) return null;

	return (
		<BgtCard title={t("rivals.title")} icon={LeftRightArrow}>
			<ul className="-mt-2 flex flex-col divide-y divide-white/10">
				{rivals.slice(0, RIVAL_COUNT).map((rival) => {
					const opponent = playerById(rival.playerId);
					return (
						<li key={rival.playerId}>
							<Link
								to="/compare"
								search={{ left: playerId, right: rival.playerId }}
								className="-mx-2 flex items-center gap-3 rounded-md px-2 py-2 transition-colors hover:bg-primary/10"
							>
								<span aria-hidden="true">
									<BgtAvatar image={opponent?.image} title={opponent?.name} size="large" shape="round" />
								</span>
								<span className="min-w-0 flex-1">
									<span className="block truncate font-semibold">{opponent?.name}</span>
									<span className="block text-xs text-white/60">{t("common:sessions", { count: rival.sessions })}</span>
								</span>
								<span
									className={cx(
										"shrink-0 font-bold tabular-nums",
										rival.wins > rival.losses ? "text-ok" : rival.wins < rival.losses ? "text-error" : "text-white/70",
									)}
								>
									<span className="sr-only">{t("rivals.record", { wins: rival.wins, losses: rival.losses })}</span>
									<span aria-hidden="true">
										{rival.wins}–{rival.losses}
									</span>
								</span>
							</Link>
						</li>
					);
				})}
			</ul>
		</BgtCard>
	);
};
