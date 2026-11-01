import { cx } from "class-variance-authority";
import { useEffect, useRef } from "react";
import { useTranslation } from "react-i18next";
import Calendar from "@/assets/icons/calendar.svg?react";
import Check from "@/assets/icons/check.svg?react";
import CircleHelp from "@/assets/icons/circle-help.svg?react";
import XIcon from "@/assets/icons/x.svg?react";
import BgtButton, { buttonVariants } from "@/components/BgtButton/BgtButton";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import { textSizeClasses } from "@/components/BgtText/textStyles";
import { GameNightRsvpState } from "@/models";

interface Props {
	playerName: string;
	response: GameNightRsvpState;
	hostNotified: boolean;
	whenAndWhere: string;
	calendarHref: string;
	calendarFileName: string;
	onChangeResponse: () => void;
}

const outcomes = {
	[GameNightRsvpState.Accepted]: {
		message: "answered-accepted",
		followUp: "followup-accepted",
		Icon: Check,
		tone: "bg-green-500/20 text-green-400",
		inCalendar: true,
	},
	[GameNightRsvpState.Maybe]: {
		message: "answered-maybe",
		followUp: "followup-maybe",
		Icon: CircleHelp,
		tone: "bg-amber-500/20 text-amber-300",
		inCalendar: true,
	},
	[GameNightRsvpState.Pending]: {
		message: "answered-pending",
		followUp: null,
		Icon: CircleHelp,
		tone: "bg-white/10 text-gray-300",
		inCalendar: false,
	},
	[GameNightRsvpState.Declined]: {
		message: "answered-declined",
		followUp: "followup-declined",
		Icon: XIcon,
		tone: "bg-white/10 text-gray-300",
		inCalendar: false,
	},
} as const;

export const RsvpSuccessView = (props: Props) => {
	const { playerName, response, hostNotified, whenAndWhere, calendarHref, calendarFileName, onChangeResponse } = props;
	const { t } = useTranslation("rsvp");
	const headingRef = useRef<HTMLHeadingElement>(null);
	const outcome = outcomes[response];

	useEffect(() => {
		headingRef.current?.focus({ preventScroll: true });
		headingRef.current?.scrollIntoView?.({ block: "center", behavior: "smooth" });
	}, []);

	return (
		<BgtCard className="gap-3" data-testid="rsvp-confirmation">
			<div className="flex items-center gap-3">
				<div className={cx("flex size-10 shrink-0 items-center justify-center rounded-full", outcome.tone)}>
					<outcome.Icon className="size-5" aria-hidden />
				</div>
				<div className="flex min-w-0 flex-col gap-0.5">
					<h2 ref={headingRef} tabIndex={-1} className={`${textSizeClasses["3"]} font-bold outline-none`}>
						{t(outcome.message, { name: playerName })}
					</h2>
					<BgtText size="2" className="first-letter:uppercase" data-testid="rsvp-when-where">
						{whenAndWhere}
					</BgtText>
					<BgtText size="2" color="gray">
						{outcome.followUp && `${t(outcome.followUp)} `}
						{hostNotified ? t("host-notified") : t("answer-saved")}
					</BgtText>
				</div>
			</div>
			<div className="flex flex-wrap gap-2">
				{outcome.inCalendar && (
					<a
						href={calendarHref}
						download={calendarFileName}
						className={buttonVariants({ variant: "primary", className: "gap-2" })}
					>
						<Calendar className="size-4" aria-hidden />
						{t("add-to-calendar")}
					</a>
				)}
				<BgtButton variant="cancel" onClick={onChangeResponse}>
					{t("change-response")}
				</BgtButton>
			</div>
		</BgtCard>
	);
};
