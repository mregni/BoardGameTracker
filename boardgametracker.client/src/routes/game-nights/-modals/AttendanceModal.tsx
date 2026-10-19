import { cx } from "class-variance-authority";
import { useMemo, useState } from "react";
import { useTranslation } from "react-i18next";
import CheckIcon from "@/assets/icons/check.svg?react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import {
	BgtDialog,
	BgtDialogContent,
	BgtDialogDescription,
	BgtDialogFooter,
	BgtDialogTitle,
} from "@/components/BgtDialog";
import type { GameNightDetail, Player } from "@/models";
import { nightTable, playedPlayerIds } from "../-utils/gameNightPlay";

interface Props {
	close: () => void;
	detail: GameNightDetail;
	players: Player[];
	isPending: boolean;
	onSave: (presentPlayerIds: number[]) => Promise<unknown>;
}

export const AttendanceModal = (props: Props) => {
	const { close, detail, players, isPending, onSave } = props;
	const { t } = useTranslation("game-nights");
	const played = useMemo(() => new Set(playedPlayerIds(detail)), [detail]);
	const finished = useMemo(
		() => new Set(detail.sessions.flatMap((session) => session.playerSessions.map((x) => x.playerId))),
		[detail],
	);
	const [present, setPresent] = useState(() => new Set([...nightTable(detail).playerIds, ...played]));
	const [ordered] = useState(() => {
		const invited = new Set(detail.gameNight.invitedPlayers.map((rsvp) => rsvp.playerId));
		const rank = (player: Player) => (present.has(player.id) ? 0 : invited.has(player.id) ? 1 : 2);
		return [...players].sort((a, b) => rank(a) - rank(b) || a.name.localeCompare(b.name));
	});

	const toggle = (playerId: number) =>
		setPresent((current) => {
			const next = new Set(current);
			if (next.has(playerId)) next.delete(playerId);
			else next.add(playerId);
			return next;
		});

	const handleSave = async () => {
		await onSave([...present]);
		close();
	};

	return (
		<BgtDialog open onClose={close}>
			<BgtDialogContent size="sheet">
				<BgtDialogTitle>{t("attendance.title")}</BgtDialogTitle>
				<BgtDialogDescription>{t("attendance.hint")}</BgtDialogDescription>
				<ul className="grid gap-1.5 sm:grid-cols-2 lg:grid-cols-3">
					{ordered.map((player) => {
						const isPresent = present.has(player.id);
						const hasPlayed = played.has(player.id);
						return (
							<li key={player.id}>
								<button
									type="button"
									aria-pressed={isPresent}
									disabled={hasPlayed}
									onClick={() => toggle(player.id)}
									className={cx(
										"flex w-full items-center gap-2.5 rounded-lg border px-2.5 py-2 text-left disabled:cursor-not-allowed",
										isPresent ? "border-green-400/50 bg-green-400/10" : "border-white/10 bg-white/5 hover:bg-white/10",
									)}
								>
									<BgtAvatar title={player.name} image={player.image} />
									<span className="flex min-w-0 flex-1 flex-col">
										<span className="truncate text-sm font-semibold text-white">{player.name}</span>
										<span className="text-xs text-gray-400">
											{finished.has(player.id)
												? t("attendance.played")
												: hasPlayed
													? t("attendance.playing")
													: isPresent
													? t("attendance.present")
													: t("attendance.absent")}
										</span>
									</span>
									{isPresent && <CheckIcon className="size-4 shrink-0 text-green-300" aria-hidden="true" />}
								</button>
							</li>
						);
					})}
				</ul>
				<BgtDialogFooter
					onCancel={close}
					confirmLabel={t("attendance.save")}
					onConfirm={handleSave}
					loading={isPending}
				/>
			</BgtDialogContent>
		</BgtDialog>
	);
};
