import { useNavigate } from "@tanstack/react-router";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { BgtEditDeleteButtons } from "@/components/BgtButton/BgtEditDeleteButtons";
import type { DataTableProps } from "@/components/BgtTable/BgtDataTable";
import { CoopResultPill } from "@/components/CoopResultPill/CoopResultPill";
import { useNumberFormat } from "@/hooks/useNumberFormat";
import { usePermissions } from "@/hooks/usePermissions";
import { useShortDuration } from "@/hooks/useShortDuration";
import type { Player, Session, Settings } from "@/models";
import { minutesIntoDay, toDisplay, toDisplayTime } from "@/utils/dateUtils";
import { SessionCommentCell } from "../-components/SessionCommentCell";
import { SessionPlayerPills } from "../-components/SessionPlayerPills";

type SessionColumns = DataTableProps<Session>["columns"];

interface Props {
	settings: Settings | undefined;
	players: Player[];
	onDelete: (sessionId: number) => void;
	subjectColumn?: SessionColumns[number];
}

export const useSessionColumns = ({ settings, players, onDelete, subjectColumn }: Props) => {
	const { t } = useTranslation("common");
	const navigate = useNavigate();
	const shortDuration = useShortDuration();
	const { decimal } = useNumberFormat();
	const { canWrite } = usePermissions();

	return useMemo<SessionColumns>(() => {
		if (!settings) return [];
		return [
			{
				id: "date",
				accessorFn: (session) => new Date(session.start).getTime(),
				cell: ({ row }) => toDisplay(row.original.start, settings.dateFormat, settings.uiLanguage),
				header: t("date"),
			},
			{
				id: "time",
				accessorFn: (session) => minutesIntoDay(session.start),
				cell: ({ row }) => toDisplayTime(row.original.start, settings.timeFormat, settings.uiLanguage),
				header: t("time"),
			},
			...(subjectColumn ? [subjectColumn] : []),
			{
				accessorKey: "minutes",
				cell: ({ row }) => shortDuration(row.original.minutes),
				header: t("duration"),
			},
			{
				id: "players",
				enableSorting: false,
				cell: ({ row }) => (
					<div className="flex flex-wrap items-center gap-1.5">
						{row.original.isCoop && <CoopResultPill won={row.original.teamWon === true} />}
						{row.original.isCoop && row.original.teamScore != null && (
							<span className="text-sm font-semibold tabular-nums">{decimal(row.original.teamScore)}</span>
						)}
						<SessionPlayerPills playerSessions={row.original.playerSessions} players={players} />
					</div>
				),
				header: t("players"),
			},
			{
				id: "comment",
				enableSorting: false,
				cell: ({ row }) => <SessionCommentCell comment={row.original.comment} />,
				header: t("comment"),
			},
			...(canWrite
				? [
						{
							id: "actions",
							enableSorting: false,
							cell: ({ row }: { row: { original: Session } }) => (
								<BgtEditDeleteButtons
									onDelete={() => onDelete(row.original.id)}
									onEdit={() => navigate({ to: `/sessions/update/${row.original.id}` })}
								/>
							),
							header: () => <div className="flex justify-end">{t("actions")}</div>,
						},
					]
				: []),
		];
	}, [settings, players, onDelete, subjectColumn, t, navigate, shortDuration, decimal, canWrite]);
};
