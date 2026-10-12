import { useTranslation } from "react-i18next";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

interface Props {
	rsvps: GameNightRsvps[];
}

const segments = [
	{ state: GameNightRsvpState.Accepted, key: "rsvp.summary.coming", className: "bg-green-400" },
	{ state: GameNightRsvpState.Pending, key: "rsvp.summary.pending", className: "bg-amber-400" },
	{ state: GameNightRsvpState.Declined, key: "rsvp.summary.declined", className: "bg-red-400" },
] as const;

export const RsvpProgress = (props: Props) => {
	const { rsvps } = props;
	const { t } = useTranslation("game-nights");
	const total = rsvps.length;

	const counts = segments.map((segment) => ({
		...segment,
		count: rsvps.filter((rsvp) => rsvp.state === segment.state).length,
	}));

	const summary = counts
		.filter((segment) => segment.count > 0)
		.map((segment) => t(segment.key, { count: segment.count }))
		.join(" · ");

	return (
		<div className="flex flex-col gap-1.5">
			<div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1">
				<BgtText size="2" weight="bold">
					{t("rsvp.invited", { count: total })}
				</BgtText>
				<BgtText size="2" color="gray" data-testid="rsvp-summary">
					{summary}
				</BgtText>
			</div>
			<div className="flex h-2 overflow-hidden rounded bg-white/10" role="img" aria-label={summary}>
				{total > 0 &&
					counts.map((segment) =>
						segment.count > 0 ? (
							<span
								key={segment.state}
								className={segment.className}
								style={{ width: `${(segment.count / total) * 100}%` }}
							/>
						) : null,
					)}
			</div>
		</div>
	);
};
