import { useNavigate } from "@tanstack/react-router";
import { useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import type { Game } from "@/models";

interface Props {
	games: Game[];
}

const RADIUS = 96;
const FULL_TURNS = 6;
const SPIN_MS = 4200;

const point = (angle: number, radius: number) => {
	const radians = (angle * Math.PI) / 180;
	return { x: radius * Math.sin(radians), y: -radius * Math.cos(radians) };
};

const segmentPath = (start: number, end: number) => {
	if (end - start >= 360) {
		return `M 0 ${-RADIUS} A ${RADIUS} ${RADIUS} 0 1 1 0 ${RADIUS} A ${RADIUS} ${RADIUS} 0 1 1 0 ${-RADIUS} Z`;
	}
	const from = point(start, RADIUS);
	const to = point(end, RADIUS);
	return `M 0 0 L ${from.x} ${from.y} A ${RADIUS} ${RADIUS} 0 ${end - start > 180 ? 1 : 0} 1 ${to.x} ${to.y} Z`;
};

const segmentCoverBox = (start: number, end: number) => {
	const corners = [{ x: 0, y: 0 }, point(start, RADIUS), point(end, RADIUS)];
	for (let angle = 0; angle <= 360; angle += 90) {
		if (angle > start && angle < end) corners.push(point(angle, RADIUS));
	}
	const xs = corners.map((corner) => corner.x);
	const ys = corners.map((corner) => corner.y);
	const size = Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys));
	return {
		x: (Math.max(...xs) + Math.min(...xs)) / 2 - size / 2,
		y: (Math.max(...ys) + Math.min(...ys)) / 2 - size / 2,
		size,
	};
};

const shortTitle = (title: string) => (title.length > 14 ? `${title.slice(0, 13)}…` : title);

export const PickWheel = ({ games }: Props) => {
	const { t } = useTranslation("games");
	const navigate = useNavigate();
	const clipPrefix = useId();
	const reduceMotion = useMediaQuery("(prefers-reduced-motion: reduce)");
	const wheelRef = useRef<SVGGElement>(null);
	const rotationRef = useRef(0);
	const [winner, setWinner] = useState<number | null>(null);
	const [spinning, setSpinning] = useState(false);

	const slice = 360 / Math.max(games.length, 1);

	const spin = () => {
		const wheel = wheelRef.current;
		if (!wheel || games.length === 0) return;
		const next = Math.floor(Math.random() * games.length);
		const from = rotationRef.current;
		const to = from - (from % 360) + FULL_TURNS * 360 + (360 - (next + 0.5) * slice);
		rotationRef.current = to;
		if (typeof wheel.animate !== "function") {
			wheel.style.transform = `rotate(${to}deg)`;
			setWinner(next);
			return;
		}
		setWinner(null);
		setSpinning(true);
		const animation = wheel.animate([{ transform: `rotate(${from}deg)` }, { transform: `rotate(${to}deg)` }], {
			duration: reduceMotion ? 0 : SPIN_MS,
			easing: "cubic-bezier(0.12, 0.8, 0.18, 1)",
			fill: "forwards",
		});
		animation.onfinish = () => {
			setSpinning(false);
			setWinner(next);
		};
	};

	const overlayClass = (index: number) => {
		if (winner === null) return "fill-black/50";
		return index === winner ? "fill-black/10" : "fill-black/75";
	};

	const picked = winner === null ? undefined : games[winner];

	return (
		<div className="flex flex-col items-center gap-4 w-full">
			<svg viewBox="-112 -112 224 224" className="w-full max-w-[480px] aspect-square" aria-hidden="true">
				<g ref={wheelRef} style={{ transformOrigin: "0 0" }}>
					{games.map((game, index) => {
						const start = index * slice;
						const box = segmentCoverBox(start, start + slice);
						const clipId = `${clipPrefix}-${game.id}`;
						return (
							<g key={game.id}>
								<clipPath id={clipId}>
									<path d={segmentPath(start, start + slice)} />
								</clipPath>
								<g clipPath={`url(#${clipId})`}>
									<rect x={-RADIUS} y={-RADIUS} width={RADIUS * 2} height={RADIUS * 2} className="fill-primary/40" />
									{game.image && (
										<image
											href={game.image}
											x={box.x}
											y={box.y}
											width={box.size}
											height={box.size}
											preserveAspectRatio="xMidYMid slice"
										/>
									)}
									<rect
										x={-RADIUS}
										y={-RADIUS}
										width={RADIUS * 2}
										height={RADIUS * 2}
										className={`${overlayClass(index)} transition-[fill] duration-500`}
									/>
								</g>
							</g>
						);
					})}
					{games.length > 1 &&
						games.map((game, index) => {
							const edge = point(index * slice, RADIUS);
							return (
								<line
									key={`divider-${game.id}`}
									x1={0}
									y1={0}
									x2={edge.x}
									y2={edge.y}
									className="stroke-background"
									strokeWidth={1.5}
								/>
							);
						})}
					{games.map((game, index) => (
						<text
							key={`label-${game.id}`}
							transform={`rotate(${index * slice + slice / 2 - 90}) translate(${RADIUS * 0.6} 0)`}
							textAnchor="middle"
							dominantBaseline="middle"
							paintOrder="stroke"
							strokeWidth={2.4}
							strokeLinejoin="round"
							className={`fill-white stroke-black/80 font-bold ${games.length > 6 ? "text-[8px]" : "text-[10px]"}`}
						>
							{shortTitle(game.title)}
						</text>
					))}
					<circle r={RADIUS + 2} fill="none" className="stroke-primary" strokeWidth={6} />
					{games.length > 1 &&
						games.map((game, index) => {
							const peg = point(index * slice, RADIUS + 2);
							return <circle key={`peg-${game.id}`} cx={peg.x} cy={peg.y} r={2.2} className="fill-white" />;
						})}
					{winner !== null && (
						<path
							d={segmentPath(winner * slice, (winner + 1) * slice)}
							fill="none"
							className="stroke-card-value"
							strokeWidth={2.5}
							strokeLinejoin="round"
						/>
					)}
				</g>
				<circle r={15} className="fill-card stroke-primary" strokeWidth={4} />
				<circle r={4} className="fill-card-value" />
				<path
					d="M -9 -111 L 9 -111 L 0 -90 Z"
					className="fill-card-value stroke-background"
					strokeWidth={1.5}
					strokeLinejoin="round"
				/>
			</svg>
			<p className="min-h-7 text-lg font-bold text-card-value text-center" aria-live="polite">
				{picked ? t("pick.result", { title: picked.title }) : spinning ? t("pick.spinning") : ""}
			</p>
			<div className="flex gap-2 w-full justify-center">
				<BgtButton disabled={spinning || games.length === 0} onClick={spin}>
					{winner === null ? t("pick.spin") : t("pick.again")}
				</BgtButton>
				{picked && (
					<BgtButton variant="cancel" onClick={() => navigate({ to: "/games/$gameId", params: { gameId: picked.id } })}>
						{t("pick.open")}
					</BgtButton>
				)}
			</div>
		</div>
	);
};
