package dev.kipters.yodemo.controller;

import java.time.ZoneOffset;
import java.time.ZonedDateTime;
import java.time.format.DateTimeFormatter;
import java.time.temporal.ChronoUnit;
import java.util.ArrayList;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.core.context.SecurityContextHolder;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import dev.kipters.yodemo.config.TokenProvider;
import dev.kipters.yodemo.dto.AuthRequest;
import dev.kipters.yodemo.dto.AuthResponse;
import dev.kipters.yodemo.service.DynamoUserService;

@RestController
@RequestMapping("/auth")
public class AuthController {

    @Autowired
    private DynamoUserService userService;

    @Autowired
    private AuthenticationManager authenticationManager;

    @Autowired
    private TokenProvider tokenProvider;

    @PostMapping("/register")
    public ResponseEntity<?> register(@RequestBody AuthRequest body) {
        userService.addUser(body.username(), body.password());
        return ResponseEntity.status(HttpStatus.CREATED).build();
    }

    @PostMapping("/login")
    public ResponseEntity<?> login(@RequestBody AuthRequest body) {
        try {
            var token = new UsernamePasswordAuthenticationToken(body.username(), body.password(), new ArrayList<>());
            var auth = authenticationManager.authenticate(token);

            final var user = userService.loadUserByUsername(body.username());

            if (user != null) {
                SecurityContextHolder.getContext().setAuthentication(auth);
                final var jwtModel = tokenProvider.generateToken(auth);
                final var expirationString = ZonedDateTime
                    .ofInstant(jwtModel.expiration().toInstant(), ZoneOffset.UTC)
                    .truncatedTo(ChronoUnit.SECONDS)
                    .format(DateTimeFormatter.ISO_DATE_TIME);

                final var result = new AuthResponse(jwtModel.token(), expirationString);

                return ResponseEntity.ok(result);
            }

            return ResponseEntity.badRequest().build();
        } catch (Exception e) {
            return ResponseEntity.badRequest().build();
        }
    }
}
