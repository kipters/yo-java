package dev.kipters.yodemo.model;

import java.util.Date;

public record JwtToken(String token, Date expiration) { }
